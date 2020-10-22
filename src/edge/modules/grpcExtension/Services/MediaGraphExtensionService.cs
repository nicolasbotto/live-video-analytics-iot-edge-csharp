using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using grpcExtension.Core;
using grpcExtension.Models;
using grpcExtension.Processors;
using Microsoft.Extensions.Logging;

namespace grpcExtension
{
    public class MediaGraphExtensionService : MediaGraphExtension.MediaGraphExtensionBase, IDisposable
    {
        private readonly ILogger _logger;
        private MemoryMappedFileMemoryManager<byte> _memoryManager;
        private MediaDescriptor _clientMediaDescriptor;
        private Memory<byte> _memory;

        public MediaGraphExtensionService(ILogger logger)
        {
            _logger = logger;
        }

        public async override Task ProcessMediaStream(IAsyncStreamReader<MediaStreamMessage> requestStream, IServerStreamWriter<MediaStreamMessage> responseStream, ServerCallContext context)
        {
            // Auto increment counter. Increases per client requests
            ulong responseSeqNum = 0;
            while (await requestStream.MoveNext())
            {
                var requestMessage = requestStream.Current;
                _logger.LogInformation($"[Received] SequenceNum: {requestMessage.SequenceNumber}");
                switch (requestMessage.PayloadCase)
                {
                    case MediaStreamMessage.PayloadOneofCase.MediaStreamDescriptor:
                        var response = ProcessMediaStreamDescriptor(requestMessage.MediaStreamDescriptor);
                        var responseMessage = new MediaStreamMessage()
                        {
                            MediaStreamDescriptor = response
                        };

                        await responseStream.WriteAsync(responseMessage);
                        break;
                    case MediaStreamMessage.PayloadOneofCase.MediaSample:
                        // Extract message IDs
                        var requestSeqNum = requestMessage.SequenceNumber;

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

                        foreach (var inference in inputSample.Inferences)
                        {
                            NormalizeInference(inference);
                        }

                        // Process image
                        var imageProcessor = new ImageProcessor(_logger);
                        var stream = new MemoryStream();
                        await stream.WriteAsync(content);
                        Image image = Image.FromStream(stream);
                        var inferencesResponse = imageProcessor.ProcessImage(image);

                        var mediaSampleResponse = new MediaSample();
                        mediaSampleResponse.Inferences.AddRange(ToMediaSampleInference(inferencesResponse));

                        var mediaStreamMessageResponse = new MediaStreamMessage()
                        {
                            SequenceNumber = ++responseSeqNum,
                            AckSequenceNumber = requestSeqNum,
                            MediaSample = mediaSampleResponse
                        };

                        await responseStream.WriteAsync(mediaStreamMessageResponse);
                        break;
                }
            }
        }

        private IEnumerable<Inference> ToMediaSampleInference(InferenceResponse inferencesResponse)
        {
            return inferencesResponse.Inferences.Select(x => new Inference()
            {
                Type = Inference.Types.InferenceType.Classification,
                Subtype = x.SubType,
                Classification = new Classification()
                {
                    Tag = new Tag()
                    {
                        Value = x.Classification.Value,
                        Confidence = (float)x.Classification.Confidence
                    }
                }
            });
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
                    _logger.LogInformation(
                        1,
                        "Using shared memory transfer. Handle: {0}, Size:{1}",
                        memoryMappedFileProperties.HandleName,
                        memoryMappedFileProperties.LengthBytes);

                    try 
                    {
                        _memoryManager = new MemoryMappedFileMemoryManager<byte>(
                        memoryMappedFileProperties.HandleName,
                        (int)memoryMappedFileProperties.LengthBytes,
                        desiredAccess: MemoryMappedFileAccess.Read);

                        _memory = _memoryManager.Memory;
                    }
                    catch(Exception ex)
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
                    _logger.LogInformation(
                        1,
                        "Using embedded frame transfer.");
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
