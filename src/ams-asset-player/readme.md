# ASP .NET core app for playing Media Services Assets

This directory contains an ASP dotnet core sample app that showcases how to playback assets recorded by LVA on IoT Edge

## Contents

Most of the files in the folder are automatically generated when you create an ASP dotnet core project. The additional files are as follows.

* **amsHelper.cs** - Contains code for invoking calls to Azure Media Services.
* **.gitignore** - Defines what git should ignore at commit time.

This sample uses Azure Media Player to host an embedded player in the browser. The code for that can be found in **./Pages/Index.cshtml** and **./Pages/Index.cshtml.cs**.

## Setup

Create a file called **appsettings.development.json** and copy the contents from **appsettings.json** into it. Provide values for all parameters under AMS section. Read [How to access Media Services API](https://docs.microsoft.com/en-us/azure/media-services/latest/access-api-howto) to understand how to get the values for these parameters.

## Running the sample

* From VS Code menu, select **View --> Run** and then click on the drop-down at the top of the "Run" pane and select **"AMS Asset Player - ASP .NET core"**.
* Hit F5 to start debugging. This will result in your browser getting launched.
* Enter the name of the Media Services asset that you would like to play and hit the Submit button.

## Next steps

Experiment with different sequence of Direct Method calls by modifying operations.json.
