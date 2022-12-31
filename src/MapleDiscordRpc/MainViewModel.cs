using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using MapleDiscordRpc.Data;
using MapleDiscordRpc.Net;
using PacketDotNet;
using ReactiveUI;
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

    public MainViewModel()
    {
        SetupAdapter();
    }

    private void SetupAdapter()
    {
        _device?.Close();

        foreach (var device in LibPcapLiveDeviceList.Instance)
        {
#if DEBUG
            if (device.Interface.FriendlyName == "이더넷")
            {
                _device = device;
                break;
            }
#else
            //TODO: 디바이스 선택
#endif
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