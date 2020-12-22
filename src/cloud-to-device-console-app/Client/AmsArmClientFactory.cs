//-----------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//     Copyright (C) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Threading.Tasks;
using C2D_Console.Helpers;
using Microsoft.Azure.Management.Media;
using Microsoft.Rest.Azure.Authentication;
using LvaAzureMediaServicesClient = Microsoft.Azure.Management.Media.LvaDev.AzureMediaServicesClient;

namespace C2D_Console.Client
{
    /// <summary>
    /// A factory class to create an implementation of <see cref="IAmsArmClient"/>.
    /// </summary>
    public static class AmsArmClientFactory
    {
        /// <summary>
        /// By default, <see cref="HttpClient"/> times out after 100 seconds. Responses should not take this long, and recent
        /// ARM related errors have been determined within a few seconds even though the runner did not receive a response.
        /// By lowering the timeout the request can be retried more times within the same amount of total time.
        /// </summary>
        internal static readonly TimeSpan HttpClientTimeout = TimeSpan.FromSeconds(40);

        /// <summary>
        /// Creates an implementation of <see cref="AmsArmClient"/> based on configuration.
        /// </summary>
        /// <param name="configuration">The <see cref="AmsArmClientConfiguration"/>.</param>
        /// <returns>The created instance of <see cref="AmsArmClient"/>.</returns>
        public static async Task<AmsArmClient> CreateAsync(AmsArmClientConfiguration configuration)
        {
            Check.NotNull(configuration, nameof(configuration));

            var aadSettings = new ActiveDirectoryServiceSettings
            {
                AuthenticationEndpoint = configuration.AmsAadAuthorityEndpointBaseUri,
                TokenAudience = new Uri(configuration.AmsAadResource),
                ValidateAuthority = true,
            };

            var clientCredentials = await ApplicationTokenProvider.LoginSilentAsync(
                configuration.AmsTenantId,
                configuration.AmsClientAadClientId,
                configuration.AmsClientAadSecret,
                aadSettings);

            // The delegating handler is not disposed by default. So we pass the handler by caller and dispose it outside.
            var amsClient = new AzureMediaServicesClient(clientCredentials)
            {
                SubscriptionId = configuration.AmsAccountSubscriptionId,
                HttpClient = { Timeout = HttpClientTimeout },
            };

            var amsLvaClient = new LvaAzureMediaServicesClient(clientCredentials)
            {
                SubscriptionId = configuration.AmsAccountSubscriptionId,
                HttpClient = { Timeout = HttpClientTimeout },
            };

            return new AmsArmClient(
                amsClient,
                amsLvaClient,
                configuration.AmsAccountResourceGroupName,
                configuration.AmsAccountName,
                configuration.AmsAccountRegion);
        }
    }
}
