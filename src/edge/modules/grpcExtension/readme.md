# gRPC Extension module

The gRPC extension module enables your own IoT Edge module to accept video frames as [protobuf](https://github.com/Azure/live-video-analytics/tree/master/contracts/grpc) messages and return results back to LVA using the inference metadata schema defined by LVA.

## Prerequisites

1. [Install Docker](http://docs.docker.com/docker-for-windows/install/) on your machine

### Design

This gPRC extension module is a .NET Core console application built to host a gRPC server to handle the [protobuf](https://github.com/Azure/live-video-analytics/tree/master/contracts/grpc) messages sent between LVA and your custom AI. LVA sends a media stream descriptor which defines what information will be sent followed by video frames to the server as a [protobuf](https://github.com/Azure/live-video-analytics/tree/master/contracts/grpc) message over the gRPC stream session. The server validates the stream descriptor, analyses the video frame, processes it using an Image Processor, and returns inference results as a [protobuf](https://github.com/Azure/live-video-analytics/tree/master/contracts/grpc) message. 
The frames can be transferred through shared memory or they can be embedded in the message. The date transfer mode can be configured in the Media Graph topology to determine how frames will be transferred.
The gRPC server supports batching frames, this is configured using the *batchSize* parameter.

### Building, publishing and running the Docker container

To build the image, use the Docker file named `Dockerfile`.

First, a couple assumptions

* We'll be using Azure Container Registry (ACR) to publish our image before distributing it
* Our local Docker container image is already loged into ACR.
* Our hypothetical ACR name is "myregistry". Your name may defer, so please update it properly in the following commands.

> If you're unfamiliar with ACR or have any questions, please follow this [demo on building and pushing an image into ACR](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-get-started-docker-cli).

`cd` onto the grpc extension's root directory 

```
sudo docker build -t grpcextension:latest .

sudo docker tag grpcextension:latest myregistry.azurecr.io/grpcextension:1

sudo docker push myregistry.azurecr.io/grpcextension:1
```

Then, from the box where the container should execute, run this command:

`sudo docker run -d -p 5001:5001 --name grpcextension myregistry.azurecr.io/grpcextension:1 --grpcBinding tcp://0.0.0.0:5001 --batchSize 1`

Let's decompose it a bit:

* `-p 5001:5001`: it's up to you where you'd like to map the containers 5001 port. You can pick whatever port fits your needs.
* `--name`: the name of the running container.
* `registry/image:tag`: replace this with the corresponding location/image:tag where you've pushed the image built from the `Dockerfile`
* `--grpcBinding`: the port the gRPC server will listen on
* `--batchSize`: the size of the batch

### Updating references into Topologies, to target the gRPC Extension Address
The topology (i.e. https://github.com/Azure/live-video-analytics/blob/master/MediaGraph/topologies/grpcExtension/topology.json) must define an gRPC Extension Address:

* gRPC Extension Address Parameter
```
      {
        "name": "grpcExtensionAddress",
        "type": "String",
        "description": "grpc LVA Extension Address",
        "default": "tcp://lvaextension:44000"
      },
```
* Configuration
```
{
    "@type": "#Microsoft.Media.MediaGraphGrpcExtension",
    "name": "grpcExtension",
    "endpoint": {
        "@type": "#Microsoft.Media.MediaGraphUnsecuredEndpoint",
        "url": "${grpcExtensionAddress}",
        "credentials": {
        "@type": "#Microsoft.Media.MediaGraphUsernamePasswordCredentials",
        "username": "${grpcExtensionUserName}",
        "password": "${grpcExtensionPassword}"
        }
    },
    "dataTransfer": {
        "mode": "sharedMemory",
        "SharedMemorySizeMiB": "5"
    },
    "image": {
        "scale": {
        "mode": "${imageScaleMode}",
        "width": "${frameWidth}",
        "height": "${frameHeight}"
        },
        "format": {
        "@type": "#Microsoft.Media.MediaGraphImageFormatEncoded",
        "encoding": "${imageEncoding}",
        "quality": "${imageQuality}"
        }
    },
    "inputs": [
        {
        "nodeName": "motionDetection"
        }
    ]
}
```

The frames can be transferred through shared memory or they can be embedded in the message. The date transfer mode can be configured in the Media Graph topology to determine how frames will be transferred. This is achieved by configuring the dataTransfer element of the MediaGraphGrpcExtension as shown below:

Embedded:
```JSON
"dataTransfer": {
    "mode": "Embedded"
}
```

Shared memory:
```JSON
"dataTransfer": {
    "mode": "sharedMemory",
    "SharedMemorySizeMiB": "20"
}
```

**Note:** When communicating over shared memory the LVA container must have its IPC mode set to shareable and container:lvaEdge for the gRPC extension module, where lvaEdge is the name of the LVA module.
LVA module:
```JSON
{
    "HostConfig": {
        "LogConfig": {
            "Config": {
                "max-size": "10m",
                "max-file": "10"
            }
        },
        "IpcMode": "shareable"
    }
}
```

gRPC extension module:
```JSON
{
    "HostConfig": {
        "LogConfig": {
            "Config": {
                "max-size": "10m",
                "max-file": "10"
            }
        },
        "IpcMode": "container:lvaEdge"
    }
}
```

First, a couple assumptions

* We'll be using Azure Container Registry (ACR) to publish our image before distributing it
* Our local Docker is already loged into ACR.
* Our hypothetical ACR name is "myregistry". Your may defer, so please update it properly along the following commands.

> If you're unfamiliar with ACR or have any questions, please follow this [demo on building and pushing an image into ACR](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-get-started-docker-cli).

`cd` onto the repo's root directory
```
sudo docker run -d -p 5001:5001 --name grpcextension myregistry.azurecr.io/grpcextension:1 --grpcBinding tcp://0.0.0.0:5001 --batchSize 1

sudo docker tag grpcextension:latest myregistry.azurecr.io/grpcextension:1

sudo docker push myregistry.azurecr.io/grpcextension:1
```

Then, from the box where the container should execute, run this command:

`sudo docker run -d -p 5001:5001 --name grpc myregistry.azurecr.io/grpcextension:1 --grpcBinding tcp://0.0.0.0:5001 --batchSize 1`

## gRPC extension container response
If successful, you will see JSON printed on your screen that looks something like this

```JSON
{
  "timestamp": 0,
  "inferences": [
    {
      "type": "classification",
      "subtype": "colorIntensity",
      "inferenceId": "",
      "relatedInferences": [],
      "classification": {
        "tag": {
          "value": "light",
          "confidence": 1
        },
        "attributes": []
      },
      "extensions": {},
      "valueCase": "classification"
    }
  ]
}
```

Terminate the container using the following Docker commands

```bash
  docker stop grpcextension
  docker rm grpcextension
```

## Upload Docker image to Azure container registry

Follow instructions in [Push and Pull Docker images  - Azure Container Registry](http://docs.microsoft.com/en-us/azure/container-registry/container-registry-get-started-docker-cli) to save your image for later use on another machine.

## Deploy as an Azure IoT Edge module

Follow instruction in [Deploy module from Azure portal](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-deploy-modules-portal) to deploy the container image as an IoT Edge module (use the IoT Edge module option).
