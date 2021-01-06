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
        private const string RtspSourceUrl = "rtsp://rtspsim:554/media/co-final.mkv";

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

                PrintMessage($"Creating topology {TopologyName}.", ConsoleColor.Yellow);
                var graphTopology = await amsClient.CreateOrUpdateGraphTopologyAsync(graphTopologyModel, true);
                PrintMessage($"Topology {TopologyName} has been created.", ConsoleColor.Green);

                // Create graph instance
                string graphInstanceName, assetName;

                graphInstanceName = assetName = string.Format(AssetNameFormat, DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd-hh-mm-ss"));

                // Create graph instance
                var graphInstanceModel = MediaGraphManager.CreateGraphInstanceModel(
                              graphInstanceName,
                              TopologyName,
                              assetName, 
                              RtspSourceUrl);

                PrintMessage($"Creating instance {graphInstanceName}.", ConsoleColor.Yellow);
                var graphInstance = await amsClient.CreateOrUpdateGraphInstanceAsync(graphInstanceModel, true);
                PrintMessage($"Instance {graphInstanceName} has been created.", ConsoleColor.Green);

                // Verify status
                if (graphInstance.State != GraphInstanceState.Inactive)
                {
                    throw new InvalidOperationException("The graph instance is in an invalid state");
                }

                // Activate graph instance
                PrintMessage($"Activating instance {graphInstanceName}.", ConsoleColor.Yellow);
                await amsClient.ActivateGraphInstanceAsync(graphInstanceName);

                // Verify graph instance Active state
                graphInstance = await amsClient.GetGraphInstanceAsync(graphInstanceName);
                
                if (graphInstance.State != GraphInstanceState.Active)
                {
                    throw new InvalidOperationException("The graph instance is in an invalid state");
                }
                PrintMessage($"Instance {graphInstanceName} has been activated.", ConsoleColor.Green);
                PrintMessage("Press Enter to continue to deactivate instance.", ConsoleColor.Yellow);
                Console.ReadLine();

                // Deactivate instance
                PrintMessage($"Deactivating instance {graphInstanceName}.", ConsoleColor.Yellow);
                await amsClient.DeactivateGraphInstanceAsync(graphInstanceName);
                PrintMessage($"Instance {graphInstanceName} has been deactivated.", ConsoleColor.Green);

                // Delete instance
                PrintMessage($"Deleting instance {graphInstanceName}.", ConsoleColor.Yellow);
                await amsClient.DeleteGraphInstanceAsync(graphInstanceName);
                PrintMessage($"Instance {graphInstanceName} has been deleted.", ConsoleColor.Green);

                // Delete topology
                PrintMessage($"Deleting topology {TopologyName}.", ConsoleColor.Yellow);
                await amsClient.DeleteGraphTopologyAsync(TopologyName);
                PrintMessage($"Topology {TopologyName} has been deleted.", ConsoleColor.Green);
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