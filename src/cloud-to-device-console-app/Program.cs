using C2D_Console.Client;
using C2D_Console.Helpers;
using Microsoft.Azure.Management.Media.LvaDev.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace C2D_Console
{
    class Program
    {
        private const string TopologyName = "CVRToAsset";
        private const string AssetNameFormat = "CVRToAsset-{0}";

        static async Task Main(string[] args)
        {
            try
            {
                // Read app configuration
                IConfigurationRoot appSettings = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();

                var clientConfig = appSettings.GetSection("AmsArmClient").Get<AmsArmClientConfiguration>();

                // Initialize the client
                using var amsClient = await AmsArmClientFactory.CreateAsync(clientConfig);

                // Create graph topology
                var graphTopologyModel = MediaGraphManager.CreateGraphTopologyModel(TopologyName);
                var graphTopology = await amsClient.CreateOrUpdateGraphTopologyAsync(graphTopologyModel, true);

                // Create graph instance
                string graphInstanceName, assetName;
                graphInstanceName = assetName = string.Format(AssetNameFormat, DateTime.Now.ToUniversalTime().ToString("yyyy_MM_dd_hh_mm_ss"));

                // Create grapth instance
                var graphInstanceModel = MediaGraphManager.CreateGraphInstanceModel(
                              graphInstanceName,
                              TopologyName,
                              assetName);

                var graphInstance = await amsClient.CreateOrUpdateGraphInstanceAsync(graphInstanceModel, true);

                // Verify status
                if (graphInstance.State != GraphInstanceState.Inactive)
                {
                    PrintMessage("The graph instance is in an invalid state", ConsoleColor.Red);
                }

                // Activate graph instance
                await amsClient.ActivateGraphInstanceAsync(graphInstanceName);

                // Verify graph instance Active state
                if (graphInstance.State != GraphInstanceState.Activating)
                {
                    PrintMessage("The graph instance is in an invalid state", ConsoleColor.Red);
                }

                PrintMessage("The topology will now be deactivated. Press Enter to continue", ConsoleColor.Yellow);
                Console.ReadLine();

                // Deactivate topology
                await amsClient.DeactivateGraphInstanceAsync(graphInstanceName);

                // Delete topology
                await amsClient.DeleteGraphTopologyAsync(TopologyName);
            }
            catch (Exception ex)
            {
                PrintMessage(ex.ToString(), ConsoleColor.Red);
            }

            static void PrintMessage(string message, ConsoleColor color)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(message);
                Console.ResetColor();
            }
        }
    }
}