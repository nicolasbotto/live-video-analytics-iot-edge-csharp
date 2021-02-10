using C2D_Console.Client;
using C2D_Console.Helpers;
using Microsoft.Azure.Management.Media.LvaDev.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Media.LiveVideoAnalytics.GraphTopologyCryptoProvider;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace C2D_Console
{
    class Program
    {
        private const string TopologyName = "CVRToAssetRelayTest";
        private const string AssetNameFormat = "CVRToAsset-{0}-{1}";
        private const string RtspSourceUrl = "rtsp://rtspsim:554/media/camera-300s.mkv";
        private const string ClaimValue = "value";
        private const string ClaimName = "test";
        private const string WebsocketUrl = "wss://{0}.device-tunnel-001.graph.{1}.media.azure.net/rtsp/{2}/rtspServerSink";

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

                var cryptoProvider = GraphTopologyCryptoProviderFactory.CreateAsymmetricCryptoProvider();
                var token = cryptoProvider.GetJwtToken(clientConfig.Audience, clientConfig.Issuer, 
                    new Guid(clientConfig.AmsAccountId), 
                    new Dictionary<string, string> { { ClaimName, ClaimValue } });

                // Initialize the client
                using var amsClient = await AmsArmClientFactory.CreateAsync(clientConfig);

                // Create graph topology
                var graphTopologyModel = MediaGraphManager.CreatePlaybackGraphTopologyModel(TopologyName, clientConfig.Audience, clientConfig.Issuer, cryptoProvider.Modulus, cryptoProvider.Exponent, ClaimName, ClaimValue);

                PrintMessage($"Creating topology {TopologyName}.", ConsoleColor.Yellow);
                var graphTopology = await amsClient.CreateOrUpdateGraphTopologyAsync(graphTopologyModel, true);
                PrintMessage($"Topology {TopologyName} has been created.", ConsoleColor.Green);

                PrintMessage("Enter the number of graph instances to create:", ConsoleColor.Yellow);
                int option;
                string input = Console.ReadLine();
                while (!int.TryParse(input, out option))
                {
                    PrintMessage("Not a valid option, try again...", ConsoleColor.Red);
                    input = Console.ReadLine();
                }
                
                List<GraphInstance> graphInstances = new List<GraphInstance>();

                for (int i = 0; i < option; i++)
                {
                    // Create graph instance
                    string graphInstanceName, assetName;

                    graphInstanceName = assetName = string.Format(AssetNameFormat, DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd-hh-mm-ss"), i);

                    PrintMessage("Enter the device id:", ConsoleColor.Yellow);
                    var deviceId = Console.ReadLine();

                    // Create graph instance
                    var graphInstanceModel = MediaGraphManager.CreateGraphInstanceModel(
                                  graphInstanceName,
                                  TopologyName,
                                  assetName,
                                  RtspSourceUrl,
                                  clientConfig.IoTHubArmId,
                                  deviceId);

                    PrintMessage($"Creating instance {graphInstanceName}.", ConsoleColor.Yellow);
                    var graphInstance = await amsClient.CreateOrUpdateGraphInstanceAsync(graphInstanceModel, true);
                    PrintMessage($"Instance {graphInstanceName} has been created.", ConsoleColor.Green);

                    // Verify status
                    if (graphInstance.State != GraphInstanceState.Inactive)
                    {
                        throw new InvalidOperationException("The graph instance is in an invalid state");
                    }

                    graphInstances.Add(graphInstance);
                }

                foreach (var graphInstance in graphInstances)
                {
                    var graphInstanceName = graphInstance.Name;
                    // Activate graph instance
                    PrintMessage($"Activating instance {graphInstanceName}.", ConsoleColor.Yellow);
                    await amsClient.ActivateGraphInstanceAsync(graphInstanceName);

                    // Verify graph instance Active state
                    var refreshedGraphInstance = await amsClient.GetGraphInstanceAsync(graphInstanceName);

                    if (refreshedGraphInstance.State != GraphInstanceState.Active)
                    {
                        throw new InvalidOperationException("The graph instance is in an invalid state");
                    }
                    
                    PrintMessage($"Instance {graphInstanceName} has been activated.", ConsoleColor.Green);
                    PrintMessage($"Web socket URL:\n {string.Format(WebsocketUrl, clientConfig.AmsAccountName, clientConfig.AmsClusterName, graphInstanceName)}", ConsoleColor.Cyan);
                }

                PrintMessage($"Token:\n {token}", ConsoleColor.Cyan);
                PrintMessage("Press Enter to continue to deactivate instance.", ConsoleColor.Yellow);
                Console.ReadLine();

                foreach (var graphInstance in graphInstances)
                {
                    var graphInstanceName = graphInstance.Name;

                    // Deactivate instance
                    PrintMessage($"Deactivating instance {graphInstanceName}.", ConsoleColor.Yellow);
                    await amsClient.DeactivateGraphInstanceAsync(graphInstanceName);
                    PrintMessage($"Instance {graphInstanceName} has been deactivated.", ConsoleColor.Green);

                    // Delete instance
                    PrintMessage($"Deleting instance {graphInstanceName}.", ConsoleColor.Yellow);
                    await amsClient.DeleteGraphInstanceAsync(graphInstanceName);
                    PrintMessage($"Instance {graphInstanceName} has been deleted.", ConsoleColor.Green);
                }

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