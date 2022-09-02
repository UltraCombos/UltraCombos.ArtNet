using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace UltraCombos.ArtNet
{
    public static class ArtNetMaster
    {
		static Dictionary<string, ArtNetServer> servers = new Dictionary<string, ArtNetServer>();

		public static ArtNetServer GetSharedServer(string ip)
		{
			ArtNetServer server;			
			if ( servers.TryGetValue( ip, out server ) == false)
			{
				try
				{
					var address = IPAddress.Parse( ip );
					var mask = Utility.GetSubnetMask( address );
					server = new ArtNetServer( address, mask );
					servers.Add( ip, server );
				}
				catch ( System.Exception e )
				{
					Debug.LogWarning( e.Message );
				}				
			}
			return server;
		}
	}
}
