//-----------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//     Copyright (C) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using LvaDevModels = Microsoft.Azure.Management.Media.LvaDev.Models;

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

        private const string RtspSourceUrlAbsoluteUri = "";

        /// <summary>
        /// Create graph topology model.
        /// </summary>
        /// <param name="graphTopologyName">Graph topology name.</param>
        /// <returns>GraphTopology model.</returns>
        public static LvaDevModels.GraphTopology CreateGraphTopologyModel(string graphTopologyName)
        {
            return new GraphTopologyModelBuilder(graphTopologyName, GraphTopologyDescription)
               .AddSource(
                    new LvaDevModels.MediaGraphRtspSource
                    {
                        Name = RtspSource,
                        Transport = "tcp",
                        Endpoint = new LvaDevModels.MediaGraphUnsecuredEndpoint
                        {
                            Url = "${" + RtspUrlParameterName + "}",
                            Credentials = new LvaDevModels.MediaGraphUsernamePasswordCredentials
                            {
                                Username = RtspUsernameParameterName,
                                Password = "${" + RtspPasswordParameterName + "}",
                            },
                        },
                    })
               .AddSink(
                    new LvaDevModels.MediaGraphAssetSink
                    {
                        Name = "AssetSink",
                        AssetNamePattern = "${" + AssetNameParameterName + "}",
                        Inputs = new List<LvaDevModels.NodeInput>
                    {
                        new LvaDevModels.NodeInput
                        {
                            NodeName = RtspSource,
                        },
                    },
                    })
               .AddParameters(
                    new List<LvaDevModels.ParameterDeclaration>
                    {
                        new LvaDevModels.ParameterDeclaration
                        {
                            Name = AssetNameParameterName,
                            Type = LvaDevModels.ParameterDeclarationType.String,
                            DefaultProperty = "defaultAsset",
                            Description = "asset name parameter",
                        },
                        new LvaDevModels.ParameterDeclaration
                        {
                            Name = RtspPasswordParameterName,
                            Type = LvaDevModels.ParameterDeclarationType.SecretString,
                            DefaultProperty = "defaultPassword",
                            Description = "rtsp password parameter",
                        },
                        new LvaDevModels.ParameterDeclaration
                        {
                            Name = RtspUrlParameterName,
                            Type = LvaDevModels.ParameterDeclarationType.String,
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
        /// <returns>GraphInstance model.</returns>
        public static LvaDevModels.GraphInstance CreateGraphInstanceModel(
            string graphInstanceName,
            string graphTopologyName,
            string assetName)
        {
            return new GraphInstanceModelBuilder(graphTopologyName, graphInstanceName, GraphInstanceDescription)
                .AddParameters(
                    new List<LvaDevModels.ParameterDefinition>
                    {
                        new LvaDevModels.ParameterDefinition
                        {
                            Name = AssetNameParameterName,
                            Value = assetName,
                        },
                        new LvaDevModels.ParameterDefinition
                        {
                            Name = RtspPasswordParameterName,
                            Value = "password",
                        },
                        new LvaDevModels.ParameterDefinition
                        {
                            Name = RtspUrlParameterName,
                            Value = RtspSourceUrlAbsoluteUri,
                        },
                    })
                .GraphInstance;
        }
    }
}
