//  Copyright 2023 Jonguk Kim
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

using System;
using System.Collections.Generic;
using MapleDiscordRpc.Data;
using PacketDotNet;

namespace MapleDiscordRpc.Net;

public sealed class MapleSession : IDisposable
{
    private readonly Dictionary<uint, byte[]> _inboundBuffer = new();
    private MapleStream? _inboundStream;
    private ushort _localPort;
    private ushort _remotePort;
    private ushort _proxyPort;
    private uint _inboundSequence;
    private int _socks5;
    private ushort _build;
    private bool _isTerminated;

    public bool MatchTcpPacket(TcpPacket tcpPacket)
    {
        if (_isTerminated) return false;
        if (tcpPacket.SourcePort == _localPort && tcpPacket.DestinationPort == (_proxyPort > 0 ? _proxyPort : _remotePort)) return true;
        if (tcpPacket.SourcePort == (_proxyPort > 0 ? _proxyPort : _remotePort) && tcpPacket.DestinationPort == _localPort) return true;
        return false;
    }

    public SessionResults BufferTcpPacket(TcpPacket tcpPacket, DateTime arrivalTime)
    {
        if (tcpPacket.Finished || tcpPacket.Reset)
        {
            return SessionResults.Terminated;
        }

        if (tcpPacket is {Synchronize: true, Acknowledgment: false})
        {
            _localPort = tcpPacket.SourcePort;
            _remotePort = tcpPacket.DestinationPort;

            return SessionResults.Continue;
        }

        if (tcpPacket is {Synchronize: true, Acknowledgment: true})
        {
            _inboundSequence = tcpPacket.SequenceNumber + 1;
            return SessionResults.Continue;
        }

        if (tcpPacket.PayloadData.Length == 0) return SessionResults.Continue;
        if (_build == 0)
        {
            var tcpData = tcpPacket.PayloadData;
            if (tcpPacket.SourcePort != _localPort) _inboundSequence += (uint) tcpData.Length;
            var length = (ushort) (BitConverter.ToUInt16(tcpData, 0) + 2);
            var headerData = new byte[tcpData.Length];
            Buffer.BlockCopy(tcpData, 0, headerData, 0, tcpData.Length);
            var pr = new PacketReader(headerData);

            if (length != tcpData.Length || tcpData.Length < 13)
            {
                if (_socks5 > 0 && _socks5 < 7)
                {
                    if (pr.ReadByte() == 5 && pr.ReadByte() == 1)
                    {
                        pr.ReadByte();
                        switch (pr.ReadByte())
                        {
                            case 1: //IPv4
                                pr.ReadInt();
                                break;
                            case 4: //IPv6
                                pr.ReadBytes(16);
                                break;
                        }

                        var ports = new byte[2];
                        for (var i = 1; i >= 0; i--)
                        {
                            ports[i] = pr.ReadByte();
                        }

                        var portr = new PacketReader(ports);
                        _proxyPort = _remotePort;
                        _remotePort = portr.ReadUShort();
                    }

                    _socks5++;
                    return SessionResults.Continue;
                }

                if (tcpData.Length == 3 && pr.ReadByte() == 5)
                {
                    _socks5 = 1;
                    return SessionResults.Continue;
                }

                return SessionResults.Terminated;
            }

            var len = pr.ReadUShort();
            var version = (uint) pr.ReadUShort();
            byte subVersion = 1;
            var patchLocation = "";

            if (_remotePort != 8484)
            {
                version = pr.ReadUInt();
            }
            else
            {
                patchLocation = pr.ReadMapleString();
            }

            var localIv = pr.ReadBytes(4);
            var remoteIv = pr.ReadBytes(4);
            var serverLocale = pr.ReadByte();

            if (serverLocale != 1)
            {
                return SessionResults.Terminated;
            }


            if (patchLocation != "")
            {
                var temp = int.Parse(patchLocation);
                version = (ushort) (temp & 0x7FFF);
                subVersion = (byte) ((temp >> 16) & 0xFF);
            }

            _build = (ushort) version;
            _inboundStream = new MapleStream(_build, serverLocale, remoteIv, subVersion, _remotePort);

            ProcessTcpPacket(tcpPacket, ref _inboundSequence, _inboundBuffer, _inboundStream, arrivalTime);
            return SessionResults.Show;
        }

        if (tcpPacket.SourcePort != _localPort)
        {
            ProcessTcpPacket(tcpPacket, ref _inboundSequence, _inboundBuffer, _inboundStream, arrivalTime);
        }

        return SessionResults.Continue;
    }

    public void ProcessTcpPacket(TcpPacket tcpPacket, ref uint sequence, IDictionary<uint, byte[]> buffers, MapleStream stream, DateTime arrivalDate)
    {
        if (tcpPacket.SequenceNumber > sequence)
        {
            while (buffers.TryGetValue(sequence, out var data))
            {
                buffers.Remove(sequence);
                stream.Append(data);
                sequence += (uint) data.Length;
            }

            if (tcpPacket.SequenceNumber > sequence) buffers[tcpPacket.SequenceNumber] = tcpPacket.PayloadData;
        }

        if (tcpPacket.SequenceNumber < sequence)
        {
            var difference = (int) (sequence - tcpPacket.SequenceNumber);
            if (difference > 0)
            {
                var data = tcpPacket.PayloadData;
                if (data.Length > difference)
                {
                    stream.Append(data, difference, data.Length - difference);
                    sequence += (uint) (data.Length - difference);
                }
            }
        }
        else if (tcpPacket.SequenceNumber == sequence)
        {
            var data = tcpPacket.PayloadData;
            stream.Append(data);
            sequence += (uint) data.Length;
        }

        try
        {
            while (stream.Read(arrivalDate) is { } packet)
            {
                switch ((PacketTypes) packet.Opcode)
                {
                    case PacketTypes.SelectWorldResult:
                    {
                        DiscordManager.Instance.ResetPresence();
                        break;
                    }
                    case PacketTypes.StatChanged:
                    {
                        packet.Skip(3);
                        var mask = (CharacterStat) packet.ReadInt();

                        if (mask.HasFlag(CharacterStat.Level))
                        {
                            var level = packet.ReadInt();
                            DiscordManager.Instance.Level = level;
                            DiscordManager.Instance.UpdatePresence();
                        }

                        if (mask.HasFlag(CharacterStat.Job))
                        {
                            var jobId = packet.ReadShort();
                            DiscordManager.Instance.Job = jobId;
                            DiscordManager.Instance.UpdatePresence();
                        }

                        break;
                    }
                    case PacketTypes.UserEnterField:
                    {
                        var channelId = packet.ReadInt();
                        packet.Skip(5);
                        var isWarp = packet.ReadByte() != 1;
                        packet.Skip(14);
                        if (packet.ReadShort() > 0)
                        {
                            packet.ReadString(out _);
                            packet.ReadString(out _);
                        }

                        if (!isWarp)
                        {
                            packet.Skip(51);
                            var name = packet.ReadString(13);
                            packet.Skip(10);
                            var level = packet.ReadInt();
                            var job = packet.ReadShort();
                            packet.Skip(43);
                            var mapId = packet.ReadInt();

                            Console.WriteLine(channelId);
                            DiscordManager.Instance.Name = name.Replace("\0", "");
                            DiscordManager.Instance.ChannelId = channelId;
                            DiscordManager.Instance.Level = level;
                            DiscordManager.Instance.Job = job;
                            DiscordManager.Instance.MapId = mapId;
                            DiscordManager.Instance.UpdatePresence();
                        }
                        else
                        {
                            packet.Skip(1);
                            var mapId = packet.ReadInt();

                            DiscordManager.Instance.MapId = mapId;
                            DiscordManager.Instance.UpdatePresence();
                        }


                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return;
        }
    }

    public void Dispose() { }
}