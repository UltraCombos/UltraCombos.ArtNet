using LXProtocols.Acn.Sockets;
using LXProtocols.ArtNet;
using LXProtocols.ArtNet.Packets;
using LXProtocols.ArtNet.Sockets;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace UltraCombos.ArtNet
{
    public class Packet
    {
        public byte sequence;
        public byte physical;
        public short universe;
        public byte[] data;
    }

    [ExecuteAlways]
    public class ArtNetReceiver : MonoBehaviour
    {
        public string m_Host = "10.0.0.100";
        IPAddress localIp;
        IPAddress localSubnetMask;

        ArtNetSocket socket;
        ArtPollReplyPacket pollReply;
        int replyCounter = 0;

        const string manufacturer = "Ultra Combos Co., Ltd.";
        bool isInitialized = false;

        ConcurrentQueue<NewPacketEventArgs<ArtNetPacket>> receivedQueue = new ConcurrentQueue<NewPacketEventArgs<ArtNetPacket>>();

        public Dictionary<short, Packet> Data = new Dictionary<short, Packet>();

        [Space]
        public UnityEvent onDataUpdated = new UnityEvent();

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
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update += () => UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= () => UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif

            if (socket != null)
            {
                socket.NewPacket -= OnNewPacket;
                socket.Shutdown(SocketShutdown.Both);
                socket = null;
            }

            isInitialized = false;
        }

        private void Update()
        {
            if (isInitialized == false)
            {
                localIp = IPAddress.Parse(m_Host);
                localSubnetMask = Utility.GetSubnetMask(localIp);
                var man = Crc16.ComputeHash(manufacturer);
                var dev = Crc16.ComputeHash(SystemInfo.deviceName);
                var rdmId = new LXProtocols.Acn.Rdm.UId(man, dev);
                socket = new ArtNetSocket(rdmId);
                socket.NewPacket += OnNewPacket;
                socket.Open(localIp, localSubnetMask);

                if (pollReply == null)
                {
                    pollReply = new ArtPollReplyPacket()
                    {
                        EstaCode = 0x7A70, // 0x7AA0
                        GoodInput = new byte[4],
                        IpAddress = localIp.GetAddressBytes(),
                        LongName = SystemInfo.deviceName,
                        ShortName = SystemInfo.deviceName,
                        MacAddress = Utility.GetPhysicalAddress().GetAddressBytes(),
                        Oem = 0x2828, // 0x04b4
                        PortCount = 1,
                        PortTypes = new byte[4],
                    };

                    var bits = new BitArray(new bool[8] { false, false, false, false, false, false, false, true });
                    pollReply.GoodInput[0] = Utility.ConvertToByte(bits);

                    bits = new BitArray(new bool[8] { true, false, true, false, false, false, true, false });
                    pollReply.PortTypes[0] = Utility.ConvertToByte(bits);
                }

                isInitialized = true;
                Debug.Log("ArtNetReveicer is initialized.");
            }

            bool isUpdated = false;
            while (receivedQueue.Count > 0)
            {
                if (receivedQueue.TryDequeue(out var res))
                {
                    switch (res.Packet.OpCode)
                    {
                        case ArtNetOpCodes.Dmx:
                            {
                                isUpdated = true;
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
                                pollReply.NodeReport = $"#0001 [{replyCounter:D4}] LXProtocols.ArtNet";
                                replyCounter = (replyCounter + 1) % 10000;
                                StartCoroutine(SendDelay(Random.value));
                            }
                            break;
                        case ArtNetOpCodes.PollReply:
                            {
                                //var reply = res.Packet as ArtPollReplyPacket;
                                //Debug.LogError($"{reply.LongName}");
                            }
                            break;
                        default:
                            {
                                //Debug.LogError(res.Packet.OpCode);
                            }
                            break;
                    }
                }
            }

            if (isUpdated)
            {
                onDataUpdated.Invoke();
            }
        }

        private void OnNewPacket(object sender, NewPacketEventArgs<ArtNetPacket> e)
        {
            receivedQueue.Enqueue(e);
        }

        private IEnumerator SendDelay(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            socket.Send(pollReply);
        }


    }


}

