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
        ///The authority endpoint base uri for AMS AAD app.
        /// </summary>
        public Uri AmsAadAuthorityEndpointBaseUri { get; set; }

        /// <summary>
        ///The tenant id for Azure Media Services.
        /// </summary>
        public string AmsTenantId { get; set; }

        /// <summary>
        ///The AAD client id for Azure Media Services.
        /// </summary>
        public string AmsClientAadClientId { get; set; }

        /// <summary>
        ///The AAD certificate for Azure Media Services.
        /// </summary>
        public string AmsClientAadSecret { get; set; }

        /// <summary>
        ///The AAD resource for Azure Media Services.
        /// </summary>
        public string AmsAadResource { get; set; }

        /// <summary>
        ///The AMS media account name.
        /// </summary>
        public string AmsAccountName { get; set; }

        /// <summary>
        ///The AMS media account resource group name.
        /// </summary>
        public string AmsAccountResourceGroupName { get; set; }

        /// <summary>
        ///The AMS media account subscription id.
        /// </summary>
        public string AmsAccountSubscriptionId { get; set; }

        /// <summary>
        ///The AMS media account region.
        /// </summary>
        public string AmsAccountRegion { get; set; }

        /// <summary>
        ///The media service base uri.
        /// </summary>
        public Uri AmsMediaServiceBaseUri { get; set; }

        /// <summary>
        /// The IoT Hub Arm Id
        /// </summary>
        public string IoTHubArmId { get; set; }

        /// <summary>
        /// The audience
        /// </summary>
        public string Audience { get; set; }

        /// <summary>
        /// The issuer
        /// </summary>
        public string Issuer { get; set; }

        /// <summary>
        /// The Ams Account Id
        /// </summary>
        public string AmsAccountId { get; set; }
    }
}
