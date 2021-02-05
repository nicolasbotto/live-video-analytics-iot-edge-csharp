// -----------------------------------------------------------------------
//  <copyright company="Microsoft Corporation">
//      Copyright (C) Microsoft Corporation. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------

using Microsoft.Azure.Management.Media.LvaDev.Models;
using System.Collections.Generic;

namespace C2D_Console.Helpers
{
    /// <summary>
    /// Class to help generate graph topologies.
    /// </summary>
    public class GraphTopologyModelBuilder
    {
        /// <summary>
        /// Gets The graph topology.
        /// </summary>
        public GraphTopology Graph { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphTopologyModelBuilder"/> class.
        /// </summary>
        /// <param name="graphTopologyName">Name of the graph topology.</param>
        /// <param name="graphTopologyDescription">Description of the graph topology.</param>
        public GraphTopologyModelBuilder(string graphTopologyName, string graphTopologyDescription = null)
        {
            Check.NotNull(graphTopologyName, nameof(graphTopologyName));
            Check.InRange(graphTopologyName.Length, 1, 32, nameof(graphTopologyName));

            Graph = new GraphTopology(new List<SourceNodeBase>(),
                new List<SinkNodeBase>(), name: graphTopologyName, parameters: new List<ParameterDeclaration>(), 
                processors: new List<ProcessorNodeBase>(), description: graphTopologyDescription);
        }

        /// <summary>
        /// Adds a Source to the topology's sources collection.
        /// </summary>
        /// <param name="source">SourceNodeBase to add.</param>
        /// <returns>Graph model builder.</returns>
        public GraphTopologyModelBuilder AddSource(SourceNodeBase source)
        {
            Graph.Sources.Add(source);
            return this;
        }

        /// <summary>
        /// Adds a Processor to the topology's processors collection.
        /// </summary>
        /// <param name="processor">ProcessorNodeBase to add.</param>
        /// <returns> Graph model builder.</returns>
        public GraphTopologyModelBuilder AddProcessor(ProcessorNodeBase processor)
        {
            Graph.Processors.Add(processor);
            return this;
        }

        /// <summary>
        /// Adds a Sink to the topology's sink collection.
        /// </summary>
        /// <param name="sink">SinkNodeBase to add.</param>
        /// <returns> Graph model builder.</returns>
        public GraphTopologyModelBuilder AddSink(SinkNodeBase sink)
        {
            Graph.Sinks.Add(sink);
            return this;
        }

        /// <summary>
        /// Adds a list of Sinks to the topology's sink collection.
        /// </summary>
        /// <param name="sink">SinkNodeBase to add.</param>
        /// <returns> Graph model builder.</returns>
        public GraphTopologyModelBuilder AddSinks(List<SinkNodeBase> sinks)
        {
            foreach (var sink in sinks)
            {
                Graph.Sinks.Add(sink);
            }
            
            return this;
        }

        /// <summary>
        /// Adds a Parameter to the topology's parameters collection.
        /// </summary>
        /// <param name="parameter">ParameterDeclaration to add.</param>
        /// <returns>Graph model builder.</returns>
        public GraphTopologyModelBuilder AddParameter(ParameterDeclaration parameter)
        {
            Graph.Parameters.Add(parameter);
            return this;
        }

        /// <summary>
        /// Adds a list of Parameters to the topology's parameters collection.
        /// </summary>
        /// <param name="parameters">List<ParameterDeclaration> to add.</param>
        /// <returns>Graph model builder.</returns>
        public GraphTopologyModelBuilder AddParameters(List<ParameterDeclaration> parameters)
        {
            foreach (var parameter in parameters)
            {
                Graph.Parameters.Add(parameter);
            }
            
            return this;
        }
    }
}
