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
		[SerializeField] string _Host = "2.0.0.100";

		ArtPollReplyPacket pollReply;
		int replyCounter = 0;
		ConcurrentQueue<NewPacketEventArgs<ArtNetPacket>> queue = new ConcurrentQueue<NewPacketEventArgs<ArtNetPacket>>();

		public string Host => _Host;
		public Dictionary<short, Packet> Data { get; private set; } = new Dictionary<short, Packet>();
		public int DataFrame { get; private set; } = -1;

		[Space]
		public UnityEvent onDataUpdated = new UnityEvent();

		private void OnEnable()
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.update += () => UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif
			Release();
			Initialize();
		}

		private void OnDisable()
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.update -= () => UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif
			Release();
		}

		private void OnValidate()
		{
			Release();
			Initialize();
		}

		private void Log(string msg, LogType type = LogType.Log)
		{
			Debug.unityLogger.Log( type, $"<b>[ArtNet Receiver]</b> {msg}" );
		}

		private void Initialize()
		{
			var server = ArtNetMaster.GetSharedServer( _Host );
			if ( server != null ) server.OnNewPacket += OnNewPacket;
		}

		private void Release()
		{
			var server = ArtNetMaster.GetSharedServer( _Host );
			if ( server != null ) server.OnNewPacket -= OnNewPacket;
		}

		private void Update()
		{
			var server = ArtNetMaster.GetSharedServer( _Host );
			if ( pollReply == null && server != null )
			{
				pollReply = new ArtPollReplyPacket()
				{
					EstaCode = 0x7A70, // 0x7AA0
					GoodInput = new byte[4],
					IpAddress = server.LocalIp.GetAddressBytes(),
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

		private void OnNewPacket(NewPacketEventArgs<ArtNetPacket> args)
		{
			queue.Enqueue( args );
		}

		private IEnumerator SendDelay(float seconds)
		{
			var server = ArtNetMaster.GetSharedServer( _Host );
			if ( server == null || pollReply == null )
			{
				yield return null;
			}
			else
			{
				yield return new WaitForSeconds( seconds );
				server.Send( pollReply );
			}
		}
	}


}

