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

// ReSharper disable UnusedAutoPropertyAccessor.Global

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using DynamicData;
using MapleDiscordRpc.Data;
using MapleDiscordRpc.Net;
using PacketDotNet;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SharpPcap;
using SharpPcap.LibPcap;

namespace MapleDiscordRpc;

public sealed class MainViewModel : ReactiveObject
{
    private readonly Dictionary<int, MapleSession> _sessions = new();
    private readonly object _queueMutex = new();
    private List<RawCapture> _packetQueue = new();
    private Thread _backgroundThread;
    private LibPcapLiveDevice? _device;

    public IList<string> NetworkDeviceList { get; } = new List<string>();

    [Reactive]
    public bool StartMinimized { get; set; } = Config.Value.StartMinimized;

    [Reactive]
    public bool ShowCharacterName { get; set; } = Config.Value.ShowCharacterName;

    [Reactive]
    public bool ShowMap { get; set; } = Config.Value.ShowMap;

    [Reactive]
    public bool ShowChannel { get; set; } = Config.Value.ShowChannel;

    [Reactive]
    public bool ShowMapleGG { get; set; } = Config.Value.ShowMapleGG;

    [Reactive]
    public int SelectedNetworkDevice { get; set; }

    public MainViewModel()
    {
        var networkDeviceNames = LibPcapLiveDeviceList.Instance
                                                      .Select(x => x.Interface.FriendlyName)
                                                      .Where(x => !string.IsNullOrEmpty(x))
                                                      .ToImmutableArray();
        NetworkDeviceList.AddRange(networkDeviceNames);
        SelectedNetworkDevice = networkDeviceNames.IndexOf(Config.Value.NetworkDevice);

        SetupAdapter();
        _ = DiscordManager.Instance;
    }

    private void SetupAdapter()
    {
        _device?.Close();

        foreach (var device in LibPcapLiveDeviceList.Instance)
        {
            if (device.Interface.FriendlyName == Config.Value.NetworkDevice)
            {
                _device = device;
                break;
            }
        }

        if (_device == null)
        {
            MessageBox.Show("패킷 캡쳐 실패", "MapleDiscordRpc", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(0);
        }

        try
        {
            _backgroundThread = new Thread(PacketCaptureWorker)
            {
                IsBackground = true,
                Name = "PacketProcessThread"
            };
            _backgroundThread.Start();
            _device.OnPacketArrival += DeviceOnOnPacketArrival;
            _device.Open(DeviceModes.Promiscuous, 1);
            _device.Filter = "tcp portrange 8484-15000";
            _device.StartCapture();
        }
        catch
        {
            _device.Open();
        }
    }

    private void DeviceOnOnPacketArrival(object sender, PacketCapture e)
    {
        lock (_queueMutex)
        {
            _packetQueue.Add(e.GetPacket());
        }
    }

    private void PacketCaptureWorker()
    {
        while (true)
        {
            var flag = true;
            lock (_queueMutex)
            {
                flag = _packetQueue.Count == 0;
            }

            if (flag)
            {
                Thread.Yield();
            }
            else
            {
                List<RawCapture> ourQueue;
                lock (_queueMutex)
                {
                    ourQueue = _packetQueue;
                    _packetQueue = new();
                }

                foreach (var packet in ourQueue)
                {
                    var tcpPacket = Packet.ParsePacket(packet.LinkLayerType, packet.Data).Extract<TcpPacket>();
                    var hash = tcpPacket.SourcePort << 16 | tcpPacket.DestinationPort;
                    var hashReversed = tcpPacket.DestinationPort << 16 | tcpPacket.SourcePort;

                    try
                    {
                        MapleSession? session = null;

                        if (tcpPacket.Synchronize && tcpPacket is {Acknowledgment: false, DestinationPort: >= 8484 and <= 15000})
                        {
                            session = new MapleSession();
                            var res = session.BufferTcpPacket(tcpPacket, packet.Timeval.Date);

                            if (res == SessionResults.Continue)
                            {
                                _sessions.Add(hash, session);
                            }
                        }
                        else
                        {
                            if (_sessions.TryGetValue(hashReversed, out session))
                            {
                                var res = session.BufferTcpPacket(tcpPacket, packet.Timeval.Date);
                                if (res is SessionResults.Show or SessionResults.Continue) continue;

                                _sessions.Remove(hashReversed);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        //TODO: 로그
                    }
                }
            }
        }
    }
}