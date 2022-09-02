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
		const string manufacturer = "Ultra Combos Co., Ltd.";

		[SerializeField] string _Host = "2.0.0.100";

		IPAddress localIp;
		IPAddress localSubnetMask;
		ArtNetSocket socket;
		ArtPollReplyPacket pollReply;
		int replyCounter = 0;
		bool isInitialized = false;
		ConcurrentQueue<NewPacketEventArgs<ArtNetPacket>> queue = new ConcurrentQueue<NewPacketEventArgs<ArtNetPacket>>();

		public string Host => _Host;
		public Dictionary<short, Packet> Data { get; private set; } = new Dictionary<short, Packet>();
		public int DataFrame { get; private set; } = -1;

		[Space]
		public UnityEvent onDataUpdated = new UnityEvent();

		#region Singleton
		private static ArtNetReceiver instance;

		public static ArtNetReceiver Instance
		{
			get
			{
				if ( instance == null )
				{
					instance = FindObjectOfType<ArtNetReceiver>();
					if ( instance == null )
					{
						GameObject go = new GameObject();
						go.name = typeof( ArtNetReceiver ).Name;
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
			Release();
		}

		private void OnDisable()
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.update -= () => UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif
			Release();
		}

		private void Log(string msg, LogType type = LogType.Log)
		{
			Debug.unityLogger.Log( type, $"<b>[ArtNet Receiver]</b> {msg}" );
		}

		private void Release()
		{
			isInitialized = false;

			if ( socket != null )
			{
				try
				{
					if ( socket.PortOpen )
					{
						socket.Shutdown( SocketShutdown.Both );
					}
					socket.NewPacket -= OnNewPacket;
				}
				catch ( System.Exception e )
				{
					Log( e.Message, LogType.Error );
				}
				
				socket = null;
			}			
		}

		private void Update()
		{
			if ( isInitialized == false )
			{
				try
				{
					localIp = IPAddress.Parse( _Host );
					localSubnetMask = Utility.GetSubnetMask( localIp );
					var manufacturerId = Crc16.ComputeHash( manufacturer );
					var deviceId = Crc16.ComputeHash( SystemInfo.deviceName );
					var rdmId = new LXProtocols.Acn.Rdm.UId( manufacturerId, deviceId );
					socket = new ArtNetSocket( rdmId );
					socket.NewPacket += OnNewPacket;
					socket.Open( localIp, localSubnetMask );

					if ( pollReply == null )
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

						var bits = new BitArray( new bool[8] { false, false, false, false, false, false, false, true } );
						pollReply.GoodInput[0] = Utility.ConvertToByte( bits );

						bits = new BitArray( new bool[8] { true, false, true, false, false, false, true, false } );
						pollReply.PortTypes[0] = Utility.ConvertToByte( bits );
					}

					if ( socket.PortOpen )
					{
						isInitialized = true;
						Log( "is initialized." );
					}
				}
				catch ( System.Exception e )
				{
					Log( e.Message, LogType.Error );
				}				
			}

			bool isUpdated = false;
			while ( queue.Count > 0 )
			{
				if ( queue.TryDequeue( out var res ) )
				{
					switch ( res.Packet.OpCode )
					{
						case ArtNetOpCodes.Dmx:
							{
								isUpdated = true;
								var dmx = res.Packet as ArtNetDmxPacket;
								short universe = dmx.Universe;
								if ( Data.ContainsKey( universe ) == false )
								{
									Data.Add( universe, new Packet()
									{
										data = new byte[512],
										//data = new NativeArray<byte>( 512, Allocator.Persistent ),
									} );
								}
								var package = Data[universe];
								package.sequence = dmx.Sequence;
								package.physical = dmx.Physical;
								package.universe = universe;
								dmx.DmxData.CopyTo( package.data, 0 );
								//package.data.CopyFrom( dmx.DmxData );
							}
							break;
						case ArtNetOpCodes.Poll:
							{
								pollReply.NodeReport = $"#0001 [{replyCounter:D4}] LXProtocols.ArtNet";
								replyCounter = (replyCounter + 1) % 10000;
								StartCoroutine( SendDelay( Random.value ) );
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
			
			if ( isUpdated )
			{
				onDataUpdated.Invoke();
				DataFrame = Time.frameCount;
			}
		}

		private void OnNewPacket(object sender, NewPacketEventArgs<ArtNetPacket> e)
		{
			queue.Enqueue( e );
		}

		private IEnumerator SendDelay(float seconds)
		{
			yield return new WaitForSeconds( seconds );
			socket.Send( pollReply );
		}


	}


}

