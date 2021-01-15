# Live Video Analytics cloud to device sample console app using LVA Cloud SDK

This directory contains a dotnet core sample app that would enable you to invoke direct methods exposed by the Live Video Analytics on IoT Edge module. A JSON file (operations.json) defines the sequence of those direct methods, and the parameters for the calls.

## Contents

| File/folder             | Description                                                   |
|-------------------------|---------------------------------------------------------------|
| `c2d-console-app.csproj`| Project file.                                                 |
| `.gitignore`            | Defines what to ignore at commit time.                        |
| `README.md`             | This README file.                                             |
| `appsettings.json`      | JSON file defining the configuration parameters               |
| `Program.cs`            | The main program file                                         |

## Setup

Create a file named appsettings.json in this folder. Add the following text and provide values for all parameters.

```JSON
"AmsArmClient": {
    "AmsAadAuthorityEndpointBaseUri": "https://login.windows-ppe.net",
    "AmsTenantId": "<your AAD tenant ID>",
    "AmsClientAadClientId": "<your AAD service principal id>",
    "AmsClientAadSecret": "<your AAD service principal secret>",
    "AmsAadResource": "https://management.core.windows.net/",
    "AmsAccountName": "<name of your Media Services account>",
    "AmsAccountResourceGroupName": "<name of your resource group>",
    "AmsAccountSubscriptionId": "<your Azure subscription id>",
    "AmsAccountRegion": "<your Medias Services account region>",
    "AmsMediaServiceBaseUri": "",
    "IoTHubArmId": ""
  }
}
```
## Running the sample

* Start a debugging session (hit F5). You will start seeing some messages printed in the TERMINAL window.
