#if UNITY_EDITOR
#define SERVER_LIST
#endif

using LXProtocols.Acn.Sockets;
using LXProtocols.ArtNet.Packets;
using LXProtocols.ArtNet.Sockets;
using System;
using System.Net;

#if SERVER_LIST
using System.Collections.Generic;
using System.Collections.ObjectModel;
using LXProtocols.Acn.Rdm;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo( "UltraCombos.ArtNet.Editor" )]
#endif

namespace UltraCombos.ArtNet
{
	public sealed class ArtNetServer : IDisposable
	{
		const string manufacturer = "Ultra Combos Co., Ltd.";

		ArtNetSocket socket;
		bool disposed;

		public delegate void NewPacket(NewPacketEventArgs<ArtNetPacket> args);
		public NewPacket OnNewPacket;

		public int Port => ArtNetSocket.Port;
		public UId RdmId => socket.RdmId;
		public bool PortOpen => socket.PortOpen;
		public IPAddress LocalIp => socket.LocalIP;
		public IPAddress LocalSubnetMask => socket.LocalSubnetMask;
		public IPAddress BroadcastAddress => socket.BroadcastAddress;
		public DateTime? LastPacket => socket.LastPacket;

		public ArtNetServer(IPAddress localIp, IPAddress localSubnetMask)
		{
			var manufacturerId = Crc16.ComputeHash( manufacturer );
			var deviceId = Crc16.ComputeHash( Environment.MachineName );
			var rdmId = new LXProtocols.Acn.Rdm.UId( manufacturerId, deviceId );
			socket = new ArtNetSocket( rdmId );
			socket.NewPacket += (e, args) => OnNewPacket?.Invoke( args );
			socket.Open( localIp, localSubnetMask );

#if SERVER_LIST
			servers.Add( this );
#endif
		}

		public void Send(ArtNetPacket packet)
		{
			socket?.Send( packet );
		}

		#region IDispose implementation
		private void Dispose(bool disposing)
		{
			if ( disposed ) return;
			disposed = true;

			if ( disposing )
			{
				if ( socket != null )
				{
					socket.Close();
					socket = null;
				}
			}
		}


		~ArtNetServer()
		{
			Dispose( disposing: false );
		}

		public void Dispose()
		{
			Dispose( disposing: true );
			GC.SuppressFinalize( this );

#if SERVER_LIST
			if ( servers != null ) servers.Remove( this );
#endif
		}
		#endregion

#if SERVER_LIST

		static List<ArtNetServer> servers = new List<ArtNetServer>( 8 );
		static ReadOnlyCollection<ArtNetServer> serversReadOnly;

		internal static ReadOnlyCollection<ArtNetServer> ServerList
		{
			get
			{
				if ( serversReadOnly == null )
					serversReadOnly = new ReadOnlyCollection<ArtNetServer>( servers );
				return serversReadOnly;
			}
		}

#endif
	}
}
