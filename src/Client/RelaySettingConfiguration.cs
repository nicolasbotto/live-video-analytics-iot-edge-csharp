// -----------------------------------------------------------------------
//  <copyright company="Microsoft Corporation">
//      Copyright (C) Microsoft Corporation. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------

namespace C2D_Console.Client
{
    /// <summary>
    /// A class to abstract the relay settings.
    /// </summary>
    public class RelaySettingConfiguration
    {
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

        /// <summary>
        /// The Ams Cluster name
        /// </summary>
        public string AmsClusterName { get; set; }

        /// <summary>
        /// The Rtsp Source Url
        /// </summary>
        public string RtspSourceUrl { get; set; }

        /// <summary>
        /// The Rtsp Username
        /// </summary>
        public string RtspUsername { get; set; }

        /// <summary>
        /// The Rtsp Password
        /// </summary>
        public string RtspPassword { get; set; }
    }
}
