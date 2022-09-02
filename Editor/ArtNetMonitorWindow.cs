using LXProtocols.Acn.Sockets;
using LXProtocols.ArtNet.Packets;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UltraCombos.ArtNet
{
	public class ArtNetMonitorWindow : EditorWindow
	{
		int dataFrame;

		short displayUniverse = -1;
		Texture2D tex;
		GUIStyle barStyle;
		GUIStyle buttonStyle;

		Color baseColor;
		Color deactiveColor;
		Color activeColor;

		List<ArtNetServer> knownServers = new List<ArtNetServer>();
		ConcurrentQueue<NewPacketEventArgs<ArtNetPacket>> queue = new ConcurrentQueue<NewPacketEventArgs<ArtNetPacket>>();
		public Dictionary<short, Packet> data { get; private set; } = new Dictionary<short, Packet>();

		[MenuItem( "Window/ArtNet Monitor" )]
		public static void ShowWindow()
		{
			GetWindow<ArtNetMonitorWindow>( "ArtNet Monitor" );
		}

		private void Update()
		{
			foreach ( var server in ArtNetServer.ServerList )
			{
				if ( knownServers.Contains( server ) ) continue;
				server.OnNewPacket += OnNewPacket;
			}

			knownServers.Clear();
			foreach ( var server in ArtNetServer.ServerList ) knownServers.Add( server );

			bool isUpdated = false;
			if ( queue.TryDequeue( out var args ) )
			{
				if ( args.Packet.OpCode == LXProtocols.ArtNet.ArtNetOpCodes.Dmx )
				{
					isUpdated = true;
					var dmx = args.Packet as ArtNetDmxPacket;
					short universe = dmx.Universe;
					if ( data.ContainsKey( universe ) == false )
					{
						data.Add( universe, new Packet()
						{
							data = new byte[512],
						} );
					}
					var package = data[universe];
					package.sequence = dmx.Sequence;
					package.physical = dmx.Physical;
					package.universe = universe;
					dmx.DmxData.CopyTo( package.data, 0 );
				}
			}

			if ( isUpdated )
			{
				Repaint();
			}
		}

		private void OnNewPacket(NewPacketEventArgs<ArtNetPacket> args)
		{
			
			queue.Enqueue( args );
		}

		private void OnGUI()
		{
			if ( data.ContainsKey( displayUniverse ) == false )
			{
				displayUniverse = -1;
			}

			if ( displayUniverse < 0 )
			{
				if ( data.Count > 0 )
				{
					displayUniverse = data.Keys.First();

					baseColor = new Color32( 217, 137, 119, 255 );
					deactiveColor = new Color32( 128, 128, 128, 255 );
					activeColor = new Color32( 217, 171, 154, 255 );
				}
				else
				{
					return;
				}
			}

			if ( tex == null )
			{
				tex = Texture2D.whiteTexture;
			}

			if ( barStyle == null )
			{
				barStyle = new GUIStyle();
				barStyle.normal.textColor = Color.black;
				barStyle.alignment = TextAnchor.MiddleCenter;
			}

			if ( buttonStyle == null )
			{
				buttonStyle = new GUIStyle( GUI.skin.button );
				buttonStyle.fontStyle = FontStyle.Bold;
				buttonStyle.normal.background = tex;
				buttonStyle.hover.background = tex;
				buttonStyle.active.background = tex;
			}

			var mtx = GUI.matrix;
			float dim = position.width / 37;
			barStyle.fontSize = Mathf.FloorToInt( dim * 0.4f );

			GUI.matrix = Matrix4x4.TRS( new Vector3( dim, dim, 0 ), Quaternion.identity, Vector3.one );

			using ( var vScp = new GUILayout.VerticalScope() )
			{
				using ( var hScp = new GUILayout.HorizontalScope() )
				{
					GUI.backgroundColor = Color.black;

					foreach ( var universe in data.Keys )
					{
						GUI.backgroundColor = displayUniverse == universe ? baseColor : deactiveColor;
						if ( GUILayout.Button( $"Universe {universe}", buttonStyle, GUILayout.Width( 100 ), GUILayout.Height( 40 ) ) )
						{
							displayUniverse = universe;
						}
					}
				}

				GUI.matrix = Matrix4x4.Translate( new Vector3( 0, 50, 0 ) ) * GUI.matrix;

				const int column = 32;
				const int channels = 512;

				var size = new Vector2( dim, dim );
				float gap = 1.1f;
				for ( int i = 0; i < channels; ++i )
				{
					float x = i % column * size.x * gap;
					float y = i / column * size.y * gap;
					var channelPos = new Rect( x, y, size.x, size.y );
					float width = size.x;
					float height = size.y * data[displayUniverse].data[i] / 255.0f;
					var valuePos = new Rect( x, y, width, height );
					GUI.DrawTexture( channelPos, tex, ScaleMode.StretchToFill, true, 1, baseColor, 0, 0 );
					GUI.DrawTexture( valuePos, tex, ScaleMode.StretchToFill, false, 1, activeColor, 0, 0 );
					GUI.Label( channelPos, $"{i:D3}", barStyle );
				}
			}

			GUI.matrix = mtx;


		}
	}
}
