// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Grpc.Core;
using GrpcExtension.Core;
using GrpcExtension.Processors;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading.Tasks;

namespace GrpcExtension
{
    public class MediaGraphExtensionService : MediaGraphExtension.MediaGraphExtensionBase, IDisposable
    {
        private readonly ILogger _logger;
        private readonly int _batchSize;
        private MemoryMappedFileMemoryManager<byte> _memoryManager;
        private MediaDescriptor _clientMediaDescriptor;
        private Memory<byte> _memory;
        
        public MediaGraphExtensionService(ILogger logger, int batchSize)
        {
            _logger = logger;
            _batchSize = batchSize;
        }

        public async override Task ProcessMediaStream(IAsyncStreamReader<MediaStreamMessage> requestStream, IServerStreamWriter<MediaStreamMessage> responseStream, ServerCallContext context)
        {
            //First message from the client is (must be) MediaStreamDescriptor
            _ = await requestStream.MoveNext();
            var requestMessage = requestStream.Current;
            _logger.LogInformation($"[Received MediaStreamDescriptor] SequenceNum: {requestMessage.SequenceNumber}");
            var response = ProcessMediaStreamDescriptor(requestMessage.MediaStreamDescriptor);
            var responseMessage = new MediaStreamMessage()
            {
                MediaStreamDescriptor = response
            };

            await responseStream.WriteAsync(responseMessage);

            // Process rest of the MediaStream message sequence
            ulong responseSeqNum = 0;
            int messageCount = 1;
            List<Image> imageBatch = new List<Image>();
            while (await requestStream.MoveNext())
            {
                try
                {
                    // Extract message IDs
                    requestMessage = requestStream.Current;
                    var requestSeqNum = requestMessage.SequenceNumber;
                    _logger.LogInformation($"[Received MediaSample] SequenceNum: {requestSeqNum}");

                    // Retrieve the sample content
                    ReadOnlyMemory<byte> content = null;
                    var inputSample = requestMessage.MediaSample;

                    switch (inputSample.ContentCase)
                    {
                        case MediaSample.ContentOneofCase.ContentReference:

                            content = _memory.Slice(
                                (int)inputSample.ContentReference.AddressOffset,
                                (int)inputSample.ContentReference.LengthBytes);

                            break;

                        case MediaSample.ContentOneofCase.ContentBytes:
                            content = inputSample.ContentBytes.Bytes.ToByteArray();
                            break;
                    }

                    var mediaSampleResponse = new MediaSample();
                    var mediaStreamMessageResponse = new MediaStreamMessage()
                    {
                        SequenceNumber = ++responseSeqNum,
                        AckSequenceNumber = requestSeqNum
                    };

                    imageBatch.Add(await GetImageFromContent(content));

                    // If batch size hasn't been reached, return dummy response
                    if (messageCount < _batchSize)
                    {
                        // Return acknoledge message
                        // mediaSampleResponse.Inferences.Add(new Inference()
                        // {
                        //     Text = new Text()
                        //     {
                        //         Value = $"Adding message: {messageCount} of {_batchSize}"
                        //     }
                        // });

                        mediaStreamMessageResponse.MediaSample = mediaSampleResponse;
                        await responseStream.WriteAsync(mediaStreamMessageResponse);
                        messageCount++;
                        continue;
                    }

                    foreach (var inference in inputSample.Inferences)
                    {
                        NormalizeInference(inference);
                    }

                    // Process image
                    var imageProcessor = new BatchImageProcessor(_logger);
                    var inferencesResponse = imageProcessor.ProcessImage(imageBatch);

                    mediaSampleResponse.Inferences.AddRange(inferencesResponse);
                    mediaStreamMessageResponse.MediaSample = mediaSampleResponse;

                    await responseStream.WriteAsync(mediaStreamMessageResponse);
                    imageBatch.Clear();
                    messageCount = 1;
                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"Error processing MediaSample message: {ex.Message}.");
                }
            }
        }

        private async Task<Image> GetImageFromContent(ReadOnlyMemory<byte> content)
        {
            var stream = new MemoryStream();
            await stream.WriteAsync(content);
            return Image.FromStream(stream);
        }

        private static void NormalizeInference(Inference inference)
        {
            if (inference.ValueCase == Inference.ValueOneofCase.None)
            {
                // Note: This will terminate the RPC call. This is okay because the inferencing engine has broken the
                // RPC contract.
                throw new RpcException(
                    new Status(
                        StatusCode.InvalidArgument,
                        $"Inference has no value case set. Inference type: {inference.Type} Inference subtype: {inference.Subtype}"));
            }

            // If the type is auto, this will overwrite it to the correct type.
            // If the type is correctly set, this will be a no-op.
            var inferenceType = inference.ValueCase switch
            {
                Inference.ValueOneofCase.Classification => Inference.Types.InferenceType.Classification,
                Inference.ValueOneofCase.Motion => Inference.Types.InferenceType.Motion,
                Inference.ValueOneofCase.Entity => Inference.Types.InferenceType.Entity,
                Inference.ValueOneofCase.Text => Inference.Types.InferenceType.Text,
                Inference.ValueOneofCase.Event => Inference.Types.InferenceType.Event,
                Inference.ValueOneofCase.Other => Inference.Types.InferenceType.Other,

                _ => throw new ArgumentException($"Inference has unrecognized value case {inference.ValueCase}"),
            };

            inference.Type = inferenceType;
        }


        /// Process the Media Stream session preamble.
        /// </summary>
        /// <param name="mediaStreamDescriptor">Media session preamble.</param>
        /// <returns>Preamble response.</returns>
        public MediaStreamDescriptor ProcessMediaStreamDescriptor(MediaStreamDescriptor mediaStreamDescriptor)
        {
            // Setup data transfer
            switch (mediaStreamDescriptor.DataTransferPropertiesCase)
            {
                case MediaStreamDescriptor.DataTransferPropertiesOneofCase.SharedMemoryBufferTransferProperties:

                    var memoryMappedFileProperties = mediaStreamDescriptor.SharedMemoryBufferTransferProperties;

                    // Create a view on the memory mapped file.
                    _logger.LogInformation($"Using shared memory transfer. Handle: {memoryMappedFileProperties.HandleName}, Size:{memoryMappedFileProperties.LengthBytes}");

                    try
                    {
                        _memoryManager = new MemoryMappedFileMemoryManager<byte>(
                        memoryMappedFileProperties.HandleName,
                        (int)memoryMappedFileProperties.LengthBytes,
                        desiredAccess: MemoryMappedFileAccess.Read);

                        _memory = _memoryManager.Memory;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error creating memory manager: {ex.Message}");

                        throw new RpcException(
                            new Status(
                                StatusCode.InvalidArgument,
                                $"Error creating memory manager: {ex.Message}"));
                    }

                    break;

                case MediaStreamDescriptor.DataTransferPropertiesOneofCase.None:
                    // Nothing to be done.
                    _logger.LogInformation("Using embedded frame transfer.");
                    break;

                default:
                    throw new RpcException(new Status(StatusCode.OutOfRange, $"Unsupported data transfer method: {mediaStreamDescriptor.DataTransferPropertiesCase}"));
            }

            // Cache the client media descriptor for this stream
            _clientMediaDescriptor = mediaStreamDescriptor.MediaDescriptor;

            // Return a empty server stream descriptor as no samples are returned (only inferences)
            return new MediaStreamDescriptor { MediaDescriptor = new MediaDescriptor { Timescale = _clientMediaDescriptor.Timescale } };
        }

        public void Dispose()
        {
            ((IDisposable)_memoryManager)?.Dispose();
        }
    }
}