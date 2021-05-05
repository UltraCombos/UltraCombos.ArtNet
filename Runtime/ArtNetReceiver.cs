using LXProtocols.Acn.Sockets;
using LXProtocols.ArtNet;
using LXProtocols.ArtNet.Packets;
using LXProtocols.ArtNet.Sockets;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using Unity.Collections;
using UnityEngine;

namespace UltraCombos.ArtNet
{
    public class Packet
    {
        public byte sequence;
        public byte physical;
        public short universe;
        public byte[] data;
    }

    public class ArtNetReceiver : MonoBehaviour
    {
        public string localIp = "10.0.0.100";
        public string localSubnetMask = "255.255.255.0";

        ArtNetSocket socket;

        ConcurrentQueue<NewPacketEventArgs<ArtNetPacket>> receivedQueue = new ConcurrentQueue<NewPacketEventArgs<ArtNetPacket>>();

        public Dictionary<short, Packet> Data = new Dictionary<short, Packet>();

        #region Singleton
        private static ArtNetReceiver instance;

        public static ArtNetReceiver Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<ArtNetReceiver>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject();
                        go.name = typeof(ArtNetReceiver).Name;
                        instance = go.AddComponent<ArtNetReceiver>();
                    }
                }
                return instance;
            }
        }
        #endregion

        private void OnEnable()
        {
            var man = Crc16.ComputeHash("Ultra Combos Co., Ltd.");
            var dev = Crc16.ComputeHash(SystemInfo.deviceName);
            var rdmId = new LXProtocols.Acn.Rdm.UId(man, dev);
            socket = new ArtNetSocket(rdmId);
            socket.NewPacket += OnNewPacket;
            socket.Open(IPAddress.Parse(localIp), IPAddress.Parse(localSubnetMask));
        }

        private void OnDisable()
        {
            if (socket != null)
            {
                socket.NewPacket -= OnNewPacket;
                socket.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                socket = null;
            }
        }

        private void Update()
        {
            if (receivedQueue.TryDequeue(out var res))
            {
                switch (res.Packet.OpCode)
                {
                    case ArtNetOpCodes.Dmx:
                        {
                            var dmx = res.Packet as ArtNetDmxPacket;
                            short universe = dmx.Universe;
                            if (Data.ContainsKey(universe) == false)
                            {
                                Data.Add(universe, new Packet()
                                {
                                    data = new byte[512],
                                    //data = new NativeArray<byte>( 512, Allocator.Persistent ),
                                });
                            }
                            var package = Data[universe];
                            package.sequence = dmx.Sequence;
                            package.physical = dmx.Physical;
                            package.universe = universe;
                            dmx.DmxData.CopyTo(package.data, 0);
                            //package.data.CopyFrom( dmx.DmxData );
                        }
                        break;
                    case ArtNetOpCodes.Poll:
                        {
                            //Debug.LogError(res.Packet.OpCode);
                        }
                        break;
                    default:
                        {
                            Debug.LogError(res.Packet.OpCode);
                        }
                        break;
                }
            }

            while (receivedQueue.Count > 0)
            {
                receivedQueue.TryDequeue(out _);
            }
        }

        private void OnNewPacket(object sender, NewPacketEventArgs<ArtNetPacket> e)
        {
            receivedQueue.Enqueue(e);
        }
    }
}

