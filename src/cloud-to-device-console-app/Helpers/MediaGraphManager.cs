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
                                Username = RtspUsernameParameterName,
                                Password = "${" + RtspPasswordParameterName + "}",
                            },
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
        /// <returns>GraphInstance model.</returns>
        public static GraphInstance CreateGraphInstanceModel(
            string graphInstanceName,
            string graphTopologyName,
            string assetName,
            string rtspSourceUrl)
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
                            Name = RtspPasswordParameterName,
                            Value = "password",
                        },
                        new ParameterDefinition
                        {
                            Name = RtspUrlParameterName,
                            Value = rtspSourceUrl,
                        },
                    })
                .GraphInstance;
        }
    }
}
