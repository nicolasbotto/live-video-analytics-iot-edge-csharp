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
    /// Class to help generate graph instances.
    /// </summary>
    public class GraphInstanceModelBuilder
    {
        /// <summary>
        /// Gets The graph instance.
        /// </summary>
        public GraphInstance GraphInstance { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphInstanceModelBuilder"/> class.
        /// </summary>
        /// <param name="graphTopologyName">Name of the graph topology.</param>
        /// <param name="graphInstanceName">Name of the graph instance.</param>
        /// <param name="graphInstanceDescription">Description of the graph instance.</param>
        public GraphInstanceModelBuilder(string graphTopologyName, string graphInstanceName, string graphInstanceDescription = null)
        {
            Check.NotNull(graphTopologyName, nameof(graphTopologyName));
            Check.InRange(graphTopologyName.Length, 1, 32, nameof(graphTopologyName));
            Check.NotNull(graphInstanceName, nameof(graphInstanceName));

            GraphInstance = new GraphInstance(graphTopologyName, name: graphInstanceName, description: graphInstanceDescription,
                parameters: new List<ParameterDefinition>());                
        }

        /// <summary>
        /// Adds a Parameter to the instance's parameters collection.
        /// </summary>
        /// <param name="parameter">ParameterDefinition to add.</param>
        /// <returns>Graph model builder.</returns>
        public GraphInstanceModelBuilder AddParameter(ParameterDefinition parameter)
        {
            GraphInstance.Parameters.Add(parameter);
            return this;
        }

        /// <summary>
        /// Adds a list of Parameters to the instance's parameters collection.
        /// </summary>
        /// <param name="parameters">List<ParameterDefinition> to add.</param>
        /// <returns>Graph model builder.</returns>
        public GraphInstanceModelBuilder AddParameters(List<ParameterDefinition> parameters)
        {
            foreach (var parameter in parameters)
            {
                GraphInstance.Parameters.Add(parameter);
            }
            
            return this;
        }
    }
}
