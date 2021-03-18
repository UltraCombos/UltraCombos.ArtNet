using Haukcode.ArtNet;
using Haukcode.ArtNet.Packets;
using Haukcode.ArtNet.Sockets;
using Haukcode.Sockets;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Unity.Collections;
using UnityEngine;

namespace UltraCombos.ArtNet
{
	public class ArtNetControl : MonoBehaviour
	{
		public string localIp = "10.0.0.100";
		public string localSubnetMask = "255.255.255.0";
		public string target = "10.0.0.100";

		NativeArray<byte> data;

		[Header( "Debug" )]
		public bool send = false;
		public bool trigger = false;
		public string info;
		public int testUniverse = 1;
		public int testChannel = 1;

		CancellationTokenSource cts = new CancellationTokenSource();
		Thread thread;

		ConcurrentQueue<ArtNetPacket> sendQueue = new ConcurrentQueue<ArtNetPacket>();
		ConcurrentQueue<NewPacketEventArgs<ArtNetPacket>> receivedQueue = new ConcurrentQueue<NewPacketEventArgs<ArtNetPacket>>();
		ConcurrentQueue<string> messages = new ConcurrentQueue<string>();

		float timestamp;
		int fps = 15;

		private void Start()
		{
			data = new NativeArray<byte>( 512, Allocator.Persistent );

			thread = new Thread( () => ThreadingFunction( target, cts.Token ) );
			thread.Start();
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
		}

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

		private void OnDestroy()
		{
			cts.Cancel();
			thread.Join();
			cts.Dispose();

			data.Dispose();
		}

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
				const int fps = 30;
				int dt = 1000 / fps;
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
	}

}

