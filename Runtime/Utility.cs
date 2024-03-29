using System.Collections;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace UltraCombos.ArtNet
{
	public static class Utility
	{
		public static byte ConvertToByte(BitArray bits)
		{
			if ( bits.Count != 8 )
			{
				throw new System.ArgumentException( "bits" );
			}
			byte[] bytes = new byte[1];
			bits.CopyTo( bytes, 0 );
			return bytes[0];
		}

		public static IPAddress GetBroadcastAddress(this IPAddress address, IPAddress subnetMask)
		{
			byte[] ipAdressBytes = address.GetAddressBytes();
			byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

			if ( ipAdressBytes.Length != subnetMaskBytes.Length )
				throw new System.ArgumentException( "Lengths of IP address and subnet mask do not match." );

			byte[] broadcastAddress = new byte[ipAdressBytes.Length];
			for ( int i = 0; i < broadcastAddress.Length; i++ )
			{
				broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255));
			}
			return new IPAddress( broadcastAddress );
		}

		public static IPAddress GetNetworkAddress(this IPAddress address, IPAddress subnetMask)
		{
			byte[] ipAdressBytes = address.GetAddressBytes();
			byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

			if ( ipAdressBytes.Length != subnetMaskBytes.Length )
				throw new System.ArgumentException( "Lengths of IP address and subnet mask do not match." );

			byte[] broadcastAddress = new byte[ipAdressBytes.Length];
			for ( int i = 0; i < broadcastAddress.Length; i++ )
			{
				broadcastAddress[i] = (byte)(ipAdressBytes[i] & (subnetMaskBytes[i]));
			}
			return new IPAddress( broadcastAddress );
		}

		public static bool IsInSameSubnet(this IPAddress address2, IPAddress address, IPAddress subnetMask)
		{
			IPAddress network1 = address.GetNetworkAddress( subnetMask );
			IPAddress network2 = address2.GetNetworkAddress( subnetMask );

			return network1.Equals( network2 );
		}

		public static IPAddress GetSubnetMask(IPAddress address)
		{
			foreach ( NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces() )
			{
				foreach ( UnicastIPAddressInformation unicastIPAddressInformation in adapter.GetIPProperties().UnicastAddresses )
				{
					if ( unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork )
					{
						if ( address.Equals( unicastIPAddressInformation.Address ) )
						{
							return unicastIPAddressInformation.IPv4Mask;
						}
					}
				}
			}
			throw new System.ArgumentException( string.Format( "Can't find subnetmask for IP address '{0}'", address ) );
			return IPAddress.Any;
		}

		public static PhysicalAddress GetPhysicalAddress()
		{
			foreach ( NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces() )
			{
				if ( adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet )
				{
					return adapter.GetPhysicalAddress();
				}
			}
			throw new System.ArgumentException( string.Format( "Can't find physical address" ) );
		}
	}
}
