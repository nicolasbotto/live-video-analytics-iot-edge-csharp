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
                var rtspSourceUrl = "rtsp://rtspsim:554/media/co-final.mkv";

                graphInstanceName = assetName = string.Format(AssetNameFormat, DateTime.Now.ToUniversalTime().ToString("yyyy_MM-dd-hh-mm-ss"));

                // Create graph instance
                var graphInstanceModel = MediaGraphManager.CreateGraphInstanceModel(
                              graphInstanceName,
                              TopologyName,
                              assetName, 
                              rtspSourceUrl);

                var graphInstance = await amsClient.CreateOrUpdateGraphInstanceAsync(graphInstanceModel, true);

                // Verify status
                if (graphInstance.State != GraphInstanceState.Inactive)
                {
                    throw new InvalidOperationException("The graph instance is in an invalid state");
                }

                // Activate graph instance
                await amsClient.ActivateGraphInstanceAsync(graphInstanceName);

                // Verify graph instance Active state
                graphInstance = await amsClient.GetGraphInstanceAsync(graphInstanceName);
                
                if (graphInstance.State != GraphInstanceState.Active)
                {
                    throw new InvalidOperationException("The graph instance is in an invalid state");
                }

                PrintMessage("The topology will now be deactivated. Press Enter to continue", ConsoleColor.Yellow);
                Console.ReadLine();

                // Deactivate instance
                await amsClient.DeactivateGraphInstanceAsync(graphInstanceName);

                // Delete instance
                await amsClient.DeleteGraphInstanceAsync(graphInstanceName);

                // Delete topology
                await amsClient.DeleteGraphTopologyAsync(TopologyName);
            }
            catch(ApiErrorException ex)
            {
                PrintMessage($"Error executing client: {ex.Body.Error.Message}", ConsoleColor.Red);
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