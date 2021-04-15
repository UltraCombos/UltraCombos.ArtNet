using Haukcode.ArtNet;
using Haukcode.ArtNet.Packets;
using Haukcode.ArtNet.Sockets;
using Haukcode.Sockets;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

namespace UltraCombos.ArtNet
{
    [System.Serializable]
    public class ArtNetUniverse
    {
        public short universe = 1;
        public bool supportSequence = true;
        public bool dirty;
        public DateTime sendingTime;
        public byte this[int ch]
        {
            set {
                data[ch] = value;
                dirty = true;
            }
            get => data[ch];
        }
        public void Trigger(int ch, int keepHighFrameNum= 10)
        {
            //trigger[ch] = TriggerState.NEED_HIGH;
            this.keepHighFrameNum[ch] = keepHighFrameNum;
        }
        public byte[] data { get; } = new byte[512];
        public ArtNetUniverse()
        {
            for (int i= 0;i< keepHighFrameNum.Length;++i)
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
	public class ArtNetControl : MonoBehaviour
	{
		public string localIp = "10.0.0.100";
		public string localSubnetMask = "255.255.255.0";
		public string target = "10.0.0.100";
        public const float RESEND_SECONDS = 4;
        
        public List<ArtNetUniverse> universeList = new List<ArtNetUniverse>() { new ArtNetUniverse()};
        
        [Header( "Debug" )]
		public bool send = false;
		public bool trigger = false;
		public string info;
		public int testUniverse = 1;
		public int testChannel = 1;

        ArtNetSocket socket;
		CancellationTokenSource cts = new CancellationTokenSource();
		//Thread thread;

		ConcurrentQueue<ArtNetPacket> sendQueue = new ConcurrentQueue<ArtNetPacket>();
		ConcurrentQueue<NewPacketEventArgs<ArtNetPacket>> receivedQueue = new ConcurrentQueue<NewPacketEventArgs<ArtNetPacket>>();
		ConcurrentQueue<string> messages = new ConcurrentQueue<string>();

        const float ARTNET_FPS = 44;

        private void Start()
		{
            socket = new ArtNetSocket() { EnableBroadcast = true };
            socket.NewPacket += (sender, e) => receivedQueue.Enqueue(e);

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
                    var count = (long)(seconds * ARTNET_FPS);
                    if (pre_count < count)
                    {
                    
                        if(verbose){ 
                            var frame = count - pre_count;
                            if (frame > 1)
                                Debug.Log($"drop {frame - 1} frame{(frame > 2 ? "s" : "")}, targetFps = {ARTNET_FPS}, fps = {real_count / seconds}");
                            else
                                Debug.Log($"targetFps = {ARTNET_FPS}, fps = {real_count / seconds}");
                        }
                        byte sequence = (byte)(real_count % 254 + 1);

                        SendPackets(sequence);

                        ++real_count;
                        pre_count = count;                        
                    }
                    else
                    {                        
                        float dt = 1000 / ARTNET_FPS;
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

        void SendPackets(byte sequence )
        {
            try
            {
                var now = DateTime.Now;
                for (int i = 0; i < universeList.Count; ++i)
                {

                    for(int ch=0;ch<universeList[i].keepHighFrameNum.Length;++ch)
                    {
                        if(universeList[i].keepHighFrameNum[ch]>0)
                        {
                            --universeList[i].keepHighFrameNum[ch];
                            if (universeList[i][ch] != 255)
                            {
                                universeList[i][ch] = 255;
                                Debug.Log($"Trigger channel {ch} to high,{universeList[i][ch]}");
                                
                            }
                        }else if(universeList[i].keepHighFrameNum[ch]==0)
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
                            socket.EnableBroadcast = true;
                            socket.Send(packet, new RdmEndPoint(ip));
                        }
                        else
                        {
                            socket.Send(packet);
                        }

                        universeList[i].dirty = false;
                    }
                }
            }catch(Exception err)
            {
                Debug.LogError(err.Message+"\n"+err.StackTrace);
            }
        }

		private void Update()
		{
			if ( messages.TryDequeue( out var s ) )
			{
				Debug.LogWarning( s );
			}

			if ( receivedQueue.TryDequeue( out var e ) )
			{
				Debug.Log( $"Received ArtNet packet with OpCode: {e.Packet.OpCode} from {e.Source}" );
				/*
				switch ( e.Packet.OpCode )
				{
					case ArtNetOpCodes.Dmx:
						{
							var input = e.Packet as ArtNetDmxPacket;
							Debug.Log( $"DMX - Sequence: 0x{input.Sequence}  Physical: {input.Physical}   Universe: {input.Universe}" );
						}
						break;
				}
				*/
			}
            /*
			if ( send )
			{
				if (Time.time - timestamp > 1.0f / fps)
				{
					timestamp = Time.time;
					
					for (int i = 0; i < data.Length; ++i )
					{
						int value = Mathf.RoundToInt( Mathf.LerpUnclamped( 128, 255, Mathf.Sin( Time.time + i * 0.5f ) ) );
						data[i] = System.Convert.ToByte( value );
					}

					Set( testUniverse, data );
				}				
			}
            
			if ( trigger )
			{
				trigger = false;
				//Trigger( testUniverse, testChannel );
				DelayTrigger( testUniverse, testChannel, 0, 2.5f );
				Debug.Log( $"ArtNet trigger: U{testUniverse}C{testChannel}" );
			}
            */
		}
        /*
		public void DelayTrigger(int universe, int channel, float delay, float duration)
		{
			StartCoroutine( DoDelayTrigger( universe, channel, delay, duration ) );
		}

		private IEnumerator DoDelayTrigger(int universe, int channel, float delay, float duration)
		{
			yield return new WaitForSeconds( delay );
			Set( universe, channel, 255 );
			yield return new WaitForSeconds( duration );
			Set( universe, channel, 0 );
		}
        */
		private void OnDestroy()
		{
			cts.Cancel();
//			thread.Join();
			cts.Dispose();

			//data.Dispose();
		}

        public void Set(ushort universe, int channel, byte value, int physical = 0)
        {
            var found = universeList.Find((u) => u.universe == universe);
            if (found == null)
            {
                Debug.LogError($"Can't find universe {universe}");
                return;
            }
            if(channel<1 || channel >512)
            {
                Debug.LogError("Channel is out of bound. "+ channel);
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
            found.Trigger(channel-1);
        }
        /*
		public void SendPacket(ArtNetDmxPacket packet)
		{
			sendQueue.Enqueue( packet );
		}
        
		public void Set(int universe, int channel, int value, int sequence = 0, int physical = 0)
		{
			var ch = Mathf.Clamp( channel - 1, 0, data.Length - 1 );
			var v = Mathf.Clamp( value, 0, 255 );

			data[ch] = System.Convert.ToByte( v );

			Set( universe, data, sequence, physical );

			info = $"set U{universe}C{channel}V{value}";
		}

		public void Set(int universe, NativeArray<byte> data, int sequence = 0, int physical = 0)
		{
			var u = Mathf.Max( 0, universe - 1 );

			SendPacket( new ArtNetDmxPacket
			{
				Sequence = System.Convert.ToByte( sequence ),
				Physical = System.Convert.ToByte( physical ),
				Universe = System.Convert.ToInt16( u ),
				DmxData = data.ToArray(),
			} );
		}

		public void Trigger(int universe, int channel, int sequence = 0, int physical = 0)
		{
			Set( universe, channel, 255, sequence, physical );
			Set( universe, channel, 0, sequence, physical );
		}

		private void ThreadingFunction(string target, object obj)
		{
			try
			{
				const int FPS = 44;
				int dt = 1000 / FPS;
				using ( var socket = new ArtNetSocket() { EnableBroadcast = true } )
				{
					socket.NewPacket += (sender, e) => receivedQueue.Enqueue( e );

					var addresses = Helper.GetAddressesFromInterfaceType();
					var addr = addresses.First();

					if ( IPAddress.TryParse( localIp, out var address ) )
					{
						addr.Address = address;
					}

					if ( IPAddress.TryParse( localSubnetMask, out var mask ) )
					{
						addr.NetMask = mask;
					}

					messages.Enqueue( $"Open socket with address: {addr.Address}, mask: {addr.NetMask}, broadcast: {socket.BroadcastAddress}" );
					//socket.Open( addr.Address, addr.NetMask );

					var token = (CancellationToken)obj;
					var watch = new System.Diagnostics.Stopwatch();
					while ( token.IsCancellationRequested == false )
					{
						watch.Restart();

						if ( sendQueue.TryDequeue( out var packet ) )
						{
							//Debug.LogError( "socket.Send( packet )" );
							if ( IPAddress.TryParse( target, out var ip ) )
							{
								socket.Send( packet, new RdmEndPoint( ip ) );
							}
							else
							{
								socket.Send( packet );
							}
						}

						while ( watch.ElapsedMilliseconds < dt )
						{
							Thread.Sleep( 1000 / 250 );
						}
					}

					socket.Close();
				}
			}
			catch ( SocketException e )
			{
				messages.Enqueue( e.Message );
			}


		}
        */
	}

}

