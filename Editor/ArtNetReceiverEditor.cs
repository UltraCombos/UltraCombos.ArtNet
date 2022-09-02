using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UltraCombos.ArtNet
{
	[CustomEditor( typeof( ArtNetReceiver ) )]
	public class ArtNetReceiverEditor : Editor
	{
		SerializedProperty _Host;
		SerializedProperty onDataUpdated;

		ArtNetReceiver receiver;

		private void OnEnable()
		{
			receiver = target as ArtNetReceiver;

			_Host = serializedObject.FindProperty( nameof( _Host ) );
			onDataUpdated = serializedObject.FindProperty( nameof( onDataUpdated ) );
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField( _Host );
			var server = ArtNetMaster.GetSharedServer( _Host.stringValue );
			if ( server == null )
			{
				EditorGUILayout.HelpBox( "The ip address provided is not available.", MessageType.Warning );
			}
			else
			{
				EditorGUILayout.LabelField( "Socket Status:" );
				using ( new EditorGUI.IndentLevelScope() )
				{
					EditorGUILayout.LabelField( $"Port: {server.Port}" );
					EditorGUILayout.LabelField( $"Rdm Id: {server.RdmId}" );
					EditorGUILayout.LabelField( $"Port Open: {server.PortOpen}" );
					EditorGUILayout.LabelField( $"Local IP: {server.LocalIp}" );
					EditorGUILayout.LabelField( $"Local Subnet Mask: {server.LocalSubnetMask}" );
					EditorGUILayout.LabelField( $"Broadcast Address: {server.BroadcastAddress}" );
					EditorGUILayout.LabelField( $"Last Packet: {server.LastPacket}" );
				}
			}
			EditorGUILayout.PropertyField( onDataUpdated );


			serializedObject.ApplyModifiedProperties();
		}
	}
}
