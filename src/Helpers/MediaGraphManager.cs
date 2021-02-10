//-----------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//     Copyright (C) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.Azure.Management.Media.LvaDev.Models;
using System.Collections.Generic;

namespace C2D_Console.Helpers
{
    /// <summary>
    /// A helper class to manage runner Media Graph operations.
    /// </summary>
    public static class MediaGraphManager
	{
		private const string GraphInstanceDescription = "Graph Instance Description";
		private const string GraphTopologyDescription = "Graph Topology Description";
		private const string RtspSource = "RtspSource";

		private const string AssetNameParameterName = "assetNameParameter";
		private const string RtspUrlParameterName = "rtspUrlParameter";
		private const string RtspPasswordParameterName = "rtspPasswordParameter";
		private const string RtspUsernameParameterName = "rtspUsernameParameter";
		private const string RtspIoTHubArmIdName = "rtspIoTHubArmIdParameter";
		private const string RtspDeviceIdName = "rtspDeviceIdParameter";

		/// <summary>
		/// Create graph topology model.
		/// </summary>
		/// <param name="graphTopologyName">Graph topology name.</param>
		/// <returns>GraphTopology model.</returns>
		public static GraphTopology CreateGraphTopologyModel(string graphTopologyName)
		{
			return new GraphTopologyModelBuilder(graphTopologyName, GraphTopologyDescription)
			   .AddSource(
					new MediaGraphRtspSource
					{
						Name = RtspSource,
						Transport = "tcp",
						Endpoint = new MediaGraphUnsecuredEndpoint
						{
							Url = "${" + RtspUrlParameterName + "}",
							Credentials = new MediaGraphUsernamePasswordCredentials
							{
								Username = "${" + RtspUsernameParameterName + "}",
								Password = "${" + RtspPasswordParameterName + "}",
							},
							Tunnel = new MediaGraphIoTSecureDeviceRemoteTunnel("${" + RtspIoTHubArmIdName + "}", "${" + RtspDeviceIdName + "}")
						},
					})
			   .AddSink(
					new MediaGraphAssetSink
					{
						Name = "AssetSink",
						AssetNamePattern = "${" + AssetNameParameterName + "}",
						Inputs = new List<NodeInput>
					{
						new NodeInput
						{
							NodeName = RtspSource,
						},
					},
					})
			   .AddParameters(
					new List<ParameterDeclaration>
					{
						new ParameterDeclaration
						{
							Name = AssetNameParameterName,
							Type = ParameterDeclarationType.String,
							DefaultProperty = "defaultAsset",
							Description = "asset name parameter",
						},
						new ParameterDeclaration
						{
							Name = RtspUsernameParameterName,
							Type = ParameterDeclarationType.String,
							DefaultProperty = "defaultUsername",
							Description = "rtsp username parameter",
						},
						new ParameterDeclaration
						{
							Name = RtspPasswordParameterName,
							Type = ParameterDeclarationType.SecretString,
							DefaultProperty = "defaultPassword",
							Description = "rtsp password parameter",
						},
						new ParameterDeclaration
						{
							Name = RtspUrlParameterName,
							Type = ParameterDeclarationType.String,
							DefaultProperty = "rtsp://microsoft.com/defaultUrl",
							Description = "rtsp url parameter",
						},
						new ParameterDeclaration
						{
							Name = RtspIoTHubArmIdName,
							Type = ParameterDeclarationType.String,
							Description = "rtsp iot hub arm id",
						},
						new ParameterDeclaration
						{
							Name = RtspDeviceIdName,
							Type = ParameterDeclarationType.String,
							Description = "rtsp device id",
						}
					})
			   .Graph;
		}

		/// <summary>
		/// Create graph topology model.
		/// </summary>
		/// <param name="graphTopologyName">Graph topology name.</param>
		/// <returns>GraphTopology model.</returns>
		public static GraphTopology CreatePlaybackGraphTopologyModel(string graphTopologyName, string audience, string issuer, string modulus, string exponent, string claimName, string claimValue)
		{
			return new GraphTopologyModelBuilder(graphTopologyName, GraphTopologyDescription)
			   .AddSource(
					new MediaGraphRtspSource
					{
						Name = RtspSource,
						Transport = "tcp",
						Endpoint = new MediaGraphUnsecuredEndpoint
						{
							Url = "${" + RtspUrlParameterName + "}",
							Credentials = new MediaGraphUsernamePasswordCredentials
							{
								Username = "${" + RtspUsernameParameterName + "}",
								Password = "${" + RtspPasswordParameterName + "}",
							},
							Tunnel = new MediaGraphIoTSecureDeviceRemoteTunnel("${" + RtspIoTHubArmIdName + "}", "${" + RtspDeviceIdName + "}")
						},
					})
			   .AddSinks(
					new List<SinkNodeBase> {
						new MediaGraphAssetSink
						{
							Name = "AssetSink",
							AssetNamePattern = "${" + AssetNameParameterName + "}",
							Inputs = new List<NodeInput>
							{
								new NodeInput
								{
									NodeName = RtspSource,
								},
							},
						},
					   new MediaGraphRtspServerSink
					   {
						   Name = "rtspServerSink",
						   Inputs = new List<NodeInput>
						   {
								new NodeInput
								{
									NodeName = RtspSource
								},
						   },
						   Endpoint = new MediaGraphUnsecuredServerEndpoint
						   {
							   Tunnel = new MediaGraphTlsWebSocketTunnel
							   {
								   AuthorizationProvider = new MediaGraphJwtTokenAuthorizationProvider
								   {
									   Audience = audience,
									   Issuer = issuer,
									   VerificationKeys = new List<MediaGraphCredentials> {
											new ContentKeyPolicyRsaTokenKey()
											{
												Exponent = exponent,
												Modulus = modulus
											}
									   },
									   AdditionalRequiredClaims = new List<MediaGraphClaim> {
											new MediaGraphClaim()
                                            {
												Name = claimName,
												Value = claimValue
                                            }
									   }
								   },
							   },
							   Url = "http://test.com", // dummy url
							   Credentials = new MediaGraphJwtSymmetricTokenKey // dummy placeholder
							   {
								   Key = "test",
							   },
						   }
					   }
					})
			   .AddParameters(
					new List<ParameterDeclaration>
					{
						new ParameterDeclaration
						{
							Name = AssetNameParameterName,
							Type = ParameterDeclarationType.String,
							DefaultProperty = "defaultAsset",
							Description = "asset name parameter",
						},
						new ParameterDeclaration
						{
							Name = RtspUsernameParameterName,
							Type = ParameterDeclarationType.String,
							DefaultProperty = "defaultUsername",
							Description = "rtsp username parameter",
						},
						new ParameterDeclaration
						{
							Name = RtspPasswordParameterName,
							Type = ParameterDeclarationType.SecretString,
							DefaultProperty = "defaultPassword",
							Description = "rtsp password parameter",
						},
						new ParameterDeclaration
						{
							Name = RtspUrlParameterName,
							Type = ParameterDeclarationType.String,
							DefaultProperty = "rtsp://microsoft.com/defaultUrl",
							Description = "rtsp url parameter",
						},
						new ParameterDeclaration
						{
							Name = RtspIoTHubArmIdName,
							Type = ParameterDeclarationType.String,
							Description = "rtsp iot hub arm id",
						},
						new ParameterDeclaration
						{
							Name = RtspDeviceIdName,
							Type = ParameterDeclarationType.String,
							Description = "rtsp device id",
						}
					})
			   .Graph;
		}

		/// <summary>
		/// Create graph instance model.
		/// </summary>
		/// <param name="settings">Media graph test settings.</param>
		/// <param name="graphInstanceName">Graph instance name.</param>
		/// <param name="graphTopologyName">Graph topology name.</param>
		/// <param name="assetName">Asset name.</param>
		/// <param name="rtspSourceUrl">Rtsp source URL.</param>
		/// <param name="rtspIotHubArmId">Rtsp source IoT Hub Arm ID.</param>
		/// <param name="rtspDeviceId">Rtsp device ID.</param>
		/// <param name="rtspUsername">Rtsp username.</param>
		/// <param name="rtspPassword">Rtsp password.</param>
		/// <returns>GraphInstance model.</returns>
		public static GraphInstance CreateGraphInstanceModel(
			string graphInstanceName,
			string graphTopologyName,
			string assetName,
			string rtspSourceUrl,
			string rtspIotHubArmId,
			string rtspDeviceId,
			string rtspUsername,
			string rtspPassword)
		{
			return new GraphInstanceModelBuilder(graphTopologyName, graphInstanceName, GraphInstanceDescription)
				.AddParameters(
					new List<ParameterDefinition>
					{
						new ParameterDefinition
						{
							Name = AssetNameParameterName,
							Value = assetName,
						},
						new ParameterDefinition
						{
							Name = RtspUsernameParameterName,
							Value = rtspUsername,
						},
						new ParameterDefinition
						{
							Name = RtspPasswordParameterName,
							Value = rtspPassword,
						},
						new ParameterDefinition
						{
							Name = RtspUrlParameterName,
							Value = rtspSourceUrl,
						},
						new ParameterDefinition
						{
							Name = RtspIoTHubArmIdName,
							Value = rtspIotHubArmId,
						},
						new ParameterDefinition
						{
							Name = RtspDeviceIdName,
							Value = rtspDeviceId,
						},
					})
				.GraphInstance;
		}
	}
}