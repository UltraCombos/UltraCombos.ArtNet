using LXProtocols.Acn.Sockets;
using LXProtocols.ArtNet.Packets;
using LXProtocols.ArtNet.Sockets;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UltraCombos.ArtNet
{
    [System.Serializable]
    public class ArtNetUniverse
    {
        public short universe = 1;
        public bool supportSequence = true;
        public bool dirty;
        public System.DateTime sendingTime;
        public byte this[int ch]
        {
            set
            {
                data[ch] = value;
                dirty = true;
            }
            get => data[ch];
        }
        public void Trigger(int ch, int keepHighFrameNum = 10)
        {
            //trigger[ch] = TriggerState.NEED_HIGH;
            this.keepHighFrameNum[ch] = keepHighFrameNum;
        }
        public byte[] data { get; } = new byte[512];
        public ArtNetUniverse()
        {
            for (int i = 0; i < keepHighFrameNum.Length; ++i)
                keepHighFrameNum[i] = -1;
        }
        //public byte[] data = new byte[512];
        /*
        public TriggerState[] trigger { get; } = new TriggerState[512];

        public enum TriggerState
        {
            NONE,
            NEED_HIGH,
            NEED_LOW
        }
        */
        public int[] keepHighFrameNum { get; } = new int[512];
    }

    public class ArtNetSender : MonoBehaviour
    {
        [Range(1, 44)]
        public float framerate = 44;
        public string target = "";
        public const float RESEND_SECONDS = 4;

        public List<ArtNetUniverse> universeList = new List<ArtNetUniverse>() { new ArtNetUniverse() };

        ArtNetSocket socket;
        CancellationTokenSource cts = new CancellationTokenSource();

        ConcurrentQueue<string> messages = new ConcurrentQueue<string>();

        private void Start()
        {
            socket = new ArtNetSocket(LXProtocols.Acn.Rdm.UId.Empty);

            Task.Factory.StartNew(() =>
            {
                bool verbose = false;
                var watch = new System.Diagnostics.Stopwatch();
                watch.Restart();

                long pre_count = 0;
                long real_count = 0;

                while (cts.IsCancellationRequested == false)
                {
                    var seconds = watch.ElapsedMilliseconds / 1000.0;
                    var count = (long)(seconds * framerate);
                    if (pre_count < count)
                    {

                        if (verbose)
                        {
                            var frame = count - pre_count;
                            if (frame > 1)
                                Debug.Log($"drop {frame - 1} frame{(frame > 2 ? "s" : "")}, targetFps = {framerate}, fps = {real_count / seconds}");
                            else
                                Debug.Log($"targetFps = {framerate}, fps = {real_count / seconds}");
                        }
                        byte sequence = (byte)(real_count % 254 + 1);

                        SendPackets(sequence);

                        ++real_count;
                        pre_count = count;
                    }
                    else
                    {
                        float dt = 1000 / framerate;
                        int millis = System.Math.Max((int)(dt / 4), 1);
                        if (verbose)
                        {
                            Debug.Log($"millis = {millis}");
                        }
                        Thread.Sleep(millis);
                    }
                }
            }, cts.Token);
        }

        private void SendPackets(byte sequence)
        {
            try
            {
                var now = System.DateTime.Now;
                for (int i = 0; i < universeList.Count; ++i)
                {

                    for (int ch = 0; ch < universeList[i].keepHighFrameNum.Length; ++ch)
                    {
                        if (universeList[i].keepHighFrameNum[ch] > 0)
                        {
                            --universeList[i].keepHighFrameNum[ch];
                            if (universeList[i][ch] != 255)
                            {
                                universeList[i][ch] = 255;
                                Debug.Log($"Trigger channel {ch} to high,{universeList[i][ch]}");

                            }
                        }
                        else if (universeList[i].keepHighFrameNum[ch] == 0)
                        {
                            --universeList[i].keepHighFrameNum[ch];
                            universeList[i][ch] = 0;
                            Debug.Log($"Trigger channel {ch} to low, {universeList[i][ch]}");

                        }
                        /*

                        switch(universeList[i].trigger[ch])
                        {
                            case ArtNetUniverse.TriggerState.NEED_HIGH:
                                universeList[i].trigger[ch] = ArtNetUniverse.TriggerState.NEED_LOW;
                                universeList[i][ch] = 255;
                                Debug.Log($"Trigger channel {ch} to high,{universeList[i][ch]}");
                                break;
                            case ArtNetUniverse.TriggerState.NEED_LOW:
                                universeList[i].trigger[ch] = ArtNetUniverse.TriggerState.NONE;
                                universeList[i][ch] = 0;
                                Debug.Log($"Trigger channel {ch} to low");
                                break;
                        }
                        */
                    }
                    //Debug.Log("universeList[0][0] = " + universeList[0][0]+ ", keepHighFrameNum = "+ universeList[i].keepHighFrameNum[0]);

                    if ((now - universeList[i].sendingTime).TotalSeconds > RESEND_SECONDS)
                    {
                        Debug.Log($"over {RESEND_SECONDS} seconds, send again!!!");
                        universeList[i].dirty = true;
                    }
                    if (universeList[i].dirty)
                    {
                        universeList[i].sendingTime = now;
                        //Debug.Log($"send data[0]={universeList[i][0]}");

                        var packet = new ArtNetDmxPacket
                        {
                            Sequence = universeList[i].supportSequence ? sequence : (byte)0,
                            Physical = 0,
                            Universe = universeList[i].universe,
                            DmxData = universeList[i].data,
                        };

                        if (IPAddress.TryParse(target, out var ip))
                        {
                            socket.EnableBroadcast = false;
                            socket.Send(packet, new RdmEndPoint(ip));
                        }
                        else
                        {
                            socket.EnableBroadcast = true;
                            socket.Send(packet);
                        }

                        universeList[i].dirty = false;
                    }
                }
            }
            catch (System.Exception err)
            {
                Debug.LogError(err.Message + "\n" + err.StackTrace);
            }
        }

        private void Update()
        {
            if (messages.TryDequeue(out var s))
            {
                Debug.LogWarning(s);
            }
        }

        private void OnDestroy()
        {
            cts.Cancel();
            cts.Dispose();

            if (socket != null)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket = null;
            }
        }

        public void Set(ushort universe, int channel, byte value, int physical = 0)
        {
            var found = universeList.Find((u) => u.universe == universe);
            if (found == null)
            {
                Debug.LogError($"Can't find universe {universe}");
                return;
            }
            if (channel < 1 || channel > 512)
            {
                Debug.LogError("Channel is out of bound. " + channel);
            }
            found[channel - 1] = value;
        }
        public void Set(int universe, byte[] data, int sequence = 0, int physical = 0)
        {
            var found = universeList.Find((u) => u.universe == universe);
            if (found == null)
            {
                Debug.LogError($"Can't find universe {universe}");
                return;
            }
            for (int i = 0; i < found.data.Length; ++i)
                found[i] = data[i];
        }

        public void Trigger(ushort universe, int channel)
        {
            var found = universeList.Find((u) => u.universe == universe);
            if (found == null)
            {
                Debug.LogError($"Can't find universe {universe}");
                return;
            }
            if (channel < 1 || channel > 512)
            {
                Debug.LogError("Channel is out of bound. " + channel);
            }
            found.Trigger(channel - 1);
        }
    }

}

