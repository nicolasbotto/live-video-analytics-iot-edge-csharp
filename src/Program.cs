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
        private const string TopologyName = "CVRToAssetRelay";
        private const string AssetNameFormat = "CVRToAsset-{0}-{1}";
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
                var relaySettings = appSettings.GetSection("RelaySettings").Get<RelaySettingConfiguration>();

                var cryptoProvider = GraphTopologyCryptoProviderFactory.CreateAsymmetricCryptoProvider();

                // Get the authorizatoin token for accessing the stream using Web Socket client
                // Important: the audience, issuer and claims used by CryptoProvider must match the ones
                // set in the MediaGraphJwtTokenAuthorizationProvider of the MediaGraphRtspServerSink.
                var token = cryptoProvider.GetJwtToken(relaySettings.Audience, relaySettings.Issuer, 
                    new Guid(relaySettings.AmsAccountId), 
                    new Dictionary<string, string> { { ClaimName, ClaimValue } });

                // Initialize the client
                using var amsClient = await AmsArmClientFactory.CreateAsync(clientConfig);

                // Check if graph topology exists
                var graphTopology = await amsClient.GetGraphTopologyAsync(TopologyName);

                if (graphTopology != null)
                {
                    PrintMessage($"A graph topology named {TopologyName} already exists.", ConsoleColor.DarkGreen);
                    PrintMessage("Enter Y to re-use it otherwise N to exit.", ConsoleColor.Yellow);
                    string userInput = Console.ReadLine();

                    if (userInput.Equals("N", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return;
                    }
                }

                // Create graph topology
                var graphTopologyModel = MediaGraphManager.CreatePlaybackGraphTopologyModel(TopologyName, relaySettings.Audience, relaySettings.Issuer, cryptoProvider.Modulus, cryptoProvider.Exponent, ClaimName, ClaimValue);

                PrintMessage($"Creating topology {TopologyName}.", ConsoleColor.Yellow);
                graphTopology = await amsClient.CreateOrUpdateGraphTopologyAsync(graphTopologyModel, true);
                PrintMessage($"Topology {TopologyName} has been created.", ConsoleColor.Green);

                PrintMessage("Enter the number of graph instances to create:", ConsoleColor.Yellow);
                int option;
                string input = Console.ReadLine();
                while (!int.TryParse(input, out option) || option < 1)
                {
                    PrintMessage("Not a valid option, you must enter a number greater than 0, try again...", ConsoleColor.Red);
                    input = Console.ReadLine();
                }
                
                List<GraphInstance> graphInstances = new List<GraphInstance>();
                PrintMessage("Enter the device id:", ConsoleColor.Yellow);
                var deviceId = Console.ReadLine();

                for (int i = 0; i < option; i++)
                {
                    // Create graph instance
                    string graphInstanceName, assetName;

                    graphInstanceName = assetName = string.Format(AssetNameFormat, DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd-hh-mm-ss"), i);

                    // Create graph instance
                    var graphInstanceModel = MediaGraphManager.CreateGraphInstanceModel(
                                  graphInstanceName,
                                  TopologyName,
                                  assetName,
                                  relaySettings.RtspSourceUrl,
                                  relaySettings.IoTHubArmId,
                                  deviceId,
                                  relaySettings.RtspUsername,
                                  relaySettings.RtspPassword);

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
                    PrintMessage($"Web socket URL:\n{string.Format(WebsocketUrl, clientConfig.AmsAccountName, relaySettings.AmsClusterName, graphInstanceName)}", ConsoleColor.Cyan);
                }

                PrintMessage($"Token:\n{token}", ConsoleColor.Cyan);
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