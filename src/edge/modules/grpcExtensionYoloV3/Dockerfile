#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY . .

RUN dotnet restore "grpcExtension.csproj"
RUN dotnet build "grpcExtension.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "grpcExtension.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Required for using System.Drawing.Common
RUN apt-get update -y && \
    apt-get install -y --no-install-recommends libgdiplus && \
    apt-get install -y wget

RUN mkdir model
RUN wget --output-document="./model/yolov3ort.onnx" https://aka.ms/Microsoft-Rocket-Video-Analytics-Platform-yolov3ort.onnx

# ONNX Runtime binaries in the CPU packages use OpenMP
RUN apt-get install libgomp1

ENTRYPOINT ["dotnet", "grpcExtension.dll"]