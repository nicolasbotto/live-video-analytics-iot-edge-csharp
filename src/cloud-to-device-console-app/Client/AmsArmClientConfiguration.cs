// -----------------------------------------------------------------------
//  <copyright company="Microsoft Corporation">
//      Copyright (C) Microsoft Corporation. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------

using System;

namespace C2D_Console.Client
{
    /// <summary>
    /// A class to abstract the configurations for AMS ARM client.
    /// </summary>
    public class AmsArmClientConfiguration
    {
        /// <summary>
        /// Gets the authority endpoint base uri for AMS AAD app.
        /// </summary>
        public Uri AmsAadAuthorityEndpointBaseUri { get; set; }

        /// <summary>
        /// Gets the tenant id for Azure Media Services.
        /// </summary>
        public string AmsTenantId { get; set; }

        /// <summary>
        /// Gets the AAD client id for Azure Media Services.
        /// </summary>
        public string AmsClientAadClientId { get; set; }

        /// <summary>
        /// Gets the AAD certificate for Azure Media Services.
        /// </summary>
        public string AmsClientAadSecret { get; set; }

        /// <summary>
        /// Gets the AAD resource for Azure Media Services.
        /// </summary>
        public string AmsAadResource { get; set; }

        /// <summary>
        /// Gets the AMS media account name.
        /// </summary>
        public string AmsAccountName { get; set; }

        /// <summary>
        /// Gets the AMS media account resource group name.
        /// </summary>
        public string AmsAccountResourceGroupName { get; set; }

        /// <summary>
        /// Gets the AMS media account subscription id.
        /// </summary>
        public string AmsAccountSubscriptionId { get; set; }

        /// <summary>
        /// Gets the AMS media account region.
        /// </summary>
        public string AmsAccountRegion { get; set; }

        /// <summary>
        /// Gets the streaming endpoint name.
        /// </summary>
        public string AmsStreamingEndpointName { get; set; }

        /// <summary>
        /// Gets the AMS ARM endpoint.
        /// </summary>
        public Uri AmsArmEndpoint { get; set; }
    }
}
