//-----------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//     Copyright (C) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using C2D_Console.Helpers;
using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.LvaDev;
using Microsoft.Azure.Management.Media.LvaDev.Models;
using Microsoft.Rest.Azure;
using Polly;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AzureMediaServicesClient = Microsoft.Azure.Management.Media.AzureMediaServicesClient;
using LvaAzureMediaServicesClient = Microsoft.Azure.Management.Media.LvaDev.AzureMediaServicesClient;

namespace C2D_Console.Client
{
    /// <summary>
    /// A class to perform operations with Azure Media Service entities.
    /// </summary>
    public class AmsArmClient : IDisposable
    {
        private const int MaxRetries = 3;
        private readonly AzureMediaServicesClient _amsClient;
        private readonly LvaAzureMediaServicesClient _amsLvaClient;
        private readonly string _accountResourceGroupName;
        private readonly string _accountName;
        private readonly ConcurrentBag<string> _graphTopologyNamesTrackedForCleanup;
        private readonly ConcurrentBag<string> _graphInstanceNamesTrackedForCleanup;

        /// <summary>
        /// Initializes a new instance of the <see cref="AmsArmClient"/> class.
        /// </summary>
        /// <param name="amsClient"> The <see cref="AzureMediaServicesClient"/>.</param>
        /// <param name="amsLvaClient">The <see cref="LvaAzureMediaServicesClient"/>.</param>
        /// <param name="resourceGroupName">The AMS account resource group name.</param>
        /// <param name="accountName">The AMS account name.</param>
        /// <param name="accountRegion">The AMS account region.</param>
        public AmsArmClient(
            AzureMediaServicesClient amsClient,
            LvaAzureMediaServicesClient amsLvaClient,
            string resourceGroupName,
            string accountName,
            string accountRegion)
        {
            Check.NotNull(amsClient, nameof(amsClient));
            Check.NotNull(amsLvaClient, nameof(amsLvaClient));
            Check.NotNullOrWhiteSpace(resourceGroupName, nameof(resourceGroupName));
            Check.NotNullOrWhiteSpace(accountName, nameof(accountName));
            Check.NotNullOrWhiteSpace(accountRegion, nameof(accountRegion));

            _amsClient = amsClient;
            _amsLvaClient = amsLvaClient;
            _accountResourceGroupName = resourceGroupName;
            _accountName = accountName;
            _graphTopologyNamesTrackedForCleanup = new ConcurrentBag<string>();
            _graphInstanceNamesTrackedForCleanup = new ConcurrentBag<string>();
        }


        /// <summary>
        /// Creates or updates a <see cref="GraphTopology" /> by name and parameters.
        /// </summary>
        /// <param name="graphTopology">The <see cref="GraphTopology"/> to create or update.</param>
        /// <param name="trackForCleanup">Flag to add newly created graph topology to bag for cleanup.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>The created or updated <see cref="GraphTopology"/>.</returns>
        public Task<GraphTopology> CreateOrUpdateGraphTopologyAsync(GraphTopology graphTopology, bool trackForCleanup = true, CancellationToken cancellationToken = default)
        {
            Check.NotNull(graphTopology, nameof(graphTopology));

            if (trackForCleanup)
            {
                _graphTopologyNamesTrackedForCleanup.Add(graphTopology.Name);
            }

            return GetTransientErrorRetryPolicy(cancellationToken).ExecuteAsync(
               async () => await _amsLvaClient.GraphTopologies.CreateOrUpdateAsync(
               _accountResourceGroupName,
               _accountName,
               graphTopology.Name,
               graphTopology,
               cancellationToken));
        }

        /// <summary>
        /// Gets all GraphTopologies.
        /// </summary>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>A list of all <see cref="GraphTopology"/> objects.</returns>
        public Task<List<GraphTopology>> GetAllGraphTopologiesAsync(CancellationToken cancellationToken = default)
        {
            return ListAllEntitiesAsync(
                () => _amsLvaClient.GraphTopologies.ListAsync(_accountResourceGroupName, _accountName, cancellationToken: cancellationToken),
                token => _amsLvaClient.GraphTopologies.ListNextAsync(token, cancellationToken),
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Gets a <see cref="GraphTopology"/> by name.
        /// </summary>
        /// <param name="name">The AMS graph topology name to get.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>The <see cref="GraphTopology"/>.</returns>
        public Task<GraphTopology> GetGraphTopologyAsync(string name, CancellationToken cancellationToken = default)
        {
            Check.NotNull(name, nameof(name));

            return GetTransientErrorRetryPolicy(cancellationToken).ExecuteAsync(
                async () => await _amsLvaClient.GraphTopologies.GetAsync(
                _accountResourceGroupName,
                _accountName,
                name,
                cancellationToken));
        }

        /// <summary>
        /// Deletes a <see cref="GraphTopology"/> by name.
        /// </summary>
        /// <param name="name">The AMS graph topology name to delete.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>A task.</returns>
        public Task DeleteGraphTopologyAsync(string name, CancellationToken cancellationToken = default)
        {
            Check.NotNullOrWhiteSpace(name, nameof(name));

            return GetTransientErrorRetryPolicy(cancellationToken).ExecuteAsync(
                async () => await _amsLvaClient.GraphTopologies.DeleteAsync(
                _accountResourceGroupName,
                _accountName,
                name,
                cancellationToken));
        }

        /// <summary>
        /// Creates or updates a <see cref="GraphInstance" /> by name and parameters.
        /// </summary>
        /// <param name="graphInstance">The <see cref="GraphInstance"/> to create or update.</param>
        /// <param name="trackForCleanup">Flag to add newly created graph instance to bag for cleanup.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>The created or updated <see cref="GraphInstance"/>.</returns>
        public Task<GraphInstance> CreateOrUpdateGraphInstanceAsync(GraphInstance graphInstance, bool trackForCleanup = true, CancellationToken cancellationToken = default)
        {
            Check.NotNull(graphInstance, nameof(graphInstance));

            if (trackForCleanup)
            {
                _graphInstanceNamesTrackedForCleanup.Add(graphInstance.Name);
            }

            return GetTransientErrorRetryPolicy(cancellationToken).ExecuteAsync(
               async () => await _amsLvaClient.GraphInstances.CreateOrUpdateAsync(
               _accountResourceGroupName,
               _accountName,
               graphInstance.Name,
               graphInstance,
               cancellationToken));
        }

        /// <summary>
        /// Gets all GraphInstances.
        /// </summary>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>A list of all <see cref="GraphInstance"/> objects.</returns>
        public Task<List<GraphInstance>> GetAllGraphInstancesAsync(CancellationToken cancellationToken = default)
        {
            return ListAllEntitiesAsync(
                () => _amsLvaClient.GraphInstances.ListAsync(_accountResourceGroupName, _accountName, cancellationToken: cancellationToken),
                token => _amsLvaClient.GraphInstances.ListNextAsync(token, cancellationToken),
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Gets a <see cref="GraphInstance"/> by name.
        /// </summary>
        /// <param name="name">The AMS graph instance name to get.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>The <see cref="GraphInstance"/>.</returns>
        public Task<GraphInstance> GetGraphInstanceAsync(string name, CancellationToken cancellationToken = default)
        {
            Check.NotNull(name, nameof(name));

            return GetTransientErrorRetryPolicy(cancellationToken).ExecuteAsync(
                async () => await _amsLvaClient.GraphInstances.GetAsync(
                _accountResourceGroupName,
                _accountName,
                name,
                cancellationToken));
        }

        /// <summary>
        /// Activates a <see cref="GraphInstance"/> by name.
        /// </summary>
        /// <param name="name">The AMS graph instance name to activate.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>A task.</returns>
        public Task ActivateGraphInstanceAsync(string name, CancellationToken cancellationToken = default)
        {
            Check.NotNull(name, nameof(name));

            return GetTransientErrorRetryPolicy(cancellationToken).ExecuteAsync(
                async () => await _amsLvaClient.GraphInstances.ActivateAsync(
                    _accountResourceGroupName,
                    _accountName,
                    name,
                    cancellationToken));
        }

        /// <summary>
        /// Deactivates a <see cref="GraphInstance"/> by name.
        /// </summary>
        /// <param name="name">The AMS graph instance name to deactivate.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>A task.</returns>
        public Task DeactivateGraphInstanceAsync(string name, CancellationToken cancellationToken = default)
        {
            Check.NotNull(name, nameof(name));

            return GetTransientErrorRetryPolicy(cancellationToken).ExecuteAsync(
                async () => await _amsLvaClient.GraphInstances.DeactivateAsync(
                    _accountResourceGroupName,
                    _accountName,
                    name,
                    cancellationToken));
        }

        /// <summary>
        /// Deletes a <see cref="GraphInstance"/> by name.
        /// </summary>
        /// <param name="name">The AMS graph instance name to delete.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>A task.</returns>
        public Task DeleteGraphInstanceAsync(string name, CancellationToken cancellationToken = default)
        {
            Check.NotNullOrWhiteSpace(name, nameof(name));

            return GetTransientErrorRetryPolicy(cancellationToken).ExecuteAsync(
                async () => await _amsLvaClient.GraphInstances.DeleteAsync(
                _accountResourceGroupName,
                _accountName,
                name,
                cancellationToken));
        }

        /// <summary>
        /// Cleanup resources.
        /// </summary>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>A task.</returns>
        public void TrackGraphInstancesForCleanup(IEnumerable<string> graphInstanceNames)
        {
            foreach (var graphInstanceName in graphInstanceNames)
            {
                _graphInstanceNamesTrackedForCleanup.Add(graphInstanceName);
            }
        }

        /// <summary>
        /// Adds graph instance names to bag for cleanup.
        /// This method is useful if we need to cleanup graph instances which we are not creating in same iteration.
        /// Consumer of the client needs to call the client's Cleanup method to clean up tracked resources.
        /// </summary>
        /// <param name="graphInstanceNames">The list of graph instance names.</param>
        public void TrackGraphTopologiesForCleanup(IEnumerable<string> graphTopologyNames)
        {
            foreach (var graphTopologyName in graphTopologyNames)
            {
                _graphTopologyNamesTrackedForCleanup.Add(graphTopologyName);
            }
        }

        /// <summary>
        /// Adds graph topology names to bag for cleanup.
        /// This method is useful if we need to cleanup graph topologies which we are not creating in same iteration.
        /// Consumer of the client needs to call the client's Cleanup method to clean up tracked resources.
        /// </summary>
        /// <param name="graphTopologyNames">The list of graph topology names.</param>
        public async Task CleanupResourcesAsync(CancellationToken cancellationToken = default)
        {
            // Failure on LVA resource cleanup will fail the test.
            // Cleanup Graph Instances.
            foreach (var graphInstanceName in _graphInstanceNamesTrackedForCleanup)
            {
                await DeactivateGraphInstanceAsync(graphInstanceName, cancellationToken);

                await DeleteGraphInstanceAsync(graphInstanceName, cancellationToken);
            }

            _graphInstanceNamesTrackedForCleanup?.Clear();

            // Cleanup Graph Topologies.
            foreach (var graphTopologyName in _graphTopologyNamesTrackedForCleanup)
            {
                await DeleteGraphTopologyAsync(graphTopologyName, cancellationToken);
            }

            _graphTopologyNamesTrackedForCleanup?.Clear();
        }

        /// <summary>
        /// Release resources
        /// </summary>
        public void Dispose()
        {
            _amsClient?.Dispose();
        }

       
        /// <summary>
        /// Pages through all the entities.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="getFirstPageFunc">The function to get the first page.</param>
        /// <param name="getPageWithContinuationTokenFunc">The function to get subsequent pages given continuation token.</param>
        /// <param name="maxIterations">The max iterations for paginating.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>A list of all entities of given type in database.</returns>
        private static async Task<List<T>> ListAllEntitiesAsync<T>(
            Func<Task<IPage<T>>> getFirstPageFunc,
            Func<string, Task<IPage<T>>> getPageWithContinuationTokenFunc,
            int maxIterations = 1000,
            CancellationToken cancellationToken = default)
        {
            var retryPolicy = GetTransientErrorRetryPolicy(cancellationToken);

            var receivedEntities = await retryPolicy.ExecuteAsync(async () => await getFirstPageFunc());
            var entitiesToReturn = new List<T>(receivedEntities);
            var iteration = 0;

            while (receivedEntities.NextPageLink != null && iteration++ < maxIterations)
            {
                receivedEntities = await retryPolicy.ExecuteAsync(async () => await getPageWithContinuationTokenFunc(receivedEntities.NextPageLink));
                entitiesToReturn.AddRange(receivedEntities);
            }

            if (receivedEntities.NextPageLink != null)
            {
                var errorMessage = $"Exceeded max iterations - {maxIterations}";

                Console.WriteLine(errorMessage);

                throw new Exception(errorMessage);
            }

            return entitiesToReturn;
        }

        private static IAsyncPolicy GetTransientErrorRetryPolicy(CancellationToken cancellationToken = default)
        {
            return Policy.Handle<HttpRequestException>().RetryAsync(MaxRetries);
        }
    }
}
