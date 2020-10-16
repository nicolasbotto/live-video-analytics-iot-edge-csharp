using Grpc.Net.Client;
using grpcExtension;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static grpcExtension.MediaGraphExtension;

namespace grpcClient
{
    class Program
    {
        const string GrpcServerEndpoint = "https://localhost:5001";
        
        static async Task Main(string[] args)
        {
            try
            {
                CancellationTokenSource source = new CancellationTokenSource();
                CancellationToken token = source.Token;
                var messageString = "This is a test";
                var messageBytes = Encoding.UTF8.GetBytes(messageString);
                var offset = 265;


                using var channel = GrpcChannel.ForAddress(GrpcServerEndpoint);

                var client = new MediaGraphExtensionClient(channel);

                var mediaDescriptorMessage = CreateMediaDescriptorMessage();
                var mediaSampleMessage = CreateMediaSampleMessage(offset, messageBytes.Length);

                mediaSampleMessage.AckSequenceNumber = 1;
                mediaSampleMessage.MediaStreamDescriptor = new MediaStreamDescriptor()
                {
                    MediaDescriptor = new MediaDescriptor()
                    {
                        VideoFrameSampleFormat = new VideoFrameSampleFormat()
                        {
                            Encoding = VideoFrameSampleFormat.Types.Encoding.Jpg,
                            PixelFormat = VideoFrameSampleFormat.Types.PixelFormat.Rgb24,
                            Dimensions = new Dimensions() { Height = 640, Width = 480 }
                        },
                        Timescale = 90000
                    },
                };
                
                var d = client.ProcessMediaStream();
                await d.RequestStream.WriteAsync(mediaSampleMessage);
                await d.ResponseStream.MoveNext(token);

                var response = d.ResponseStream.Current;


                mediaSampleMessage.SequenceNumber =+ 1;
                mediaSampleMessage.MediaStreamDescriptor = null;
                var path = @"C:\Work\MS\LVA\motion-face-detect\motionfacedetect\lva\images\David1.jpg";

                var originalImage = (Bitmap)Bitmap.FromFile(path);
                Google.Protobuf.ByteString data = Google.Protobuf.ByteString.CopyFrom(GetBytes(originalImage));
                    
                var mediaSampleReq = new MediaSample();
                var content = new ContentBytes();
                content.Bytes = data;
                mediaSampleReq.ContentBytes = content;
                mediaSampleMessage.MediaSample = mediaSampleReq;
                await d.RequestStream.WriteAsync(mediaSampleMessage);
                await d.ResponseStream.MoveNext(token);

                response = d.ResponseStream.Current;

                var result = JsonConvert.SerializeObject(response.MediaSample.Inferences);

                await d.RequestStream.CompleteAsync();
                Console.WriteLine(result);

                    

                
                Console.WriteLine("Finished");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally {
                Console.WriteLine("Finally");
            }
        }

        private static byte[] GetBytes(Bitmap image)
        {
            MemoryStream stream = new MemoryStream();
            image.Save(stream, ImageFormat.Jpeg);
            return stream.ToArray();
        }

        private static MediaStreamMessage CreateMediaSampleMessage(int offset, int lentghBytes)
        {
            return new MediaStreamMessage
            {
                MediaSample = new MediaSample
                {
                    ContentReference = new ContentReference
                    {
                        AddressOffset = (uint)offset,
                        LengthBytes = (uint)lentghBytes,
                    },
                },
            };
        }

        private static MediaStreamMessage CreateMediaDescriptorMessage()
        {
            return new MediaStreamMessage
            {
                MediaStreamDescriptor = new MediaStreamDescriptor
                {
                    MediaDescriptor = new MediaDescriptor
                    {
                        Timescale = 90000,
                        VideoFrameSampleFormat = new VideoFrameSampleFormat
                        {
                            Encoding = VideoFrameSampleFormat.Types.Encoding.Raw,
                            PixelFormat = VideoFrameSampleFormat.Types.PixelFormat.Rgba,
                        },
                    },
                },
            };
        }
    }
}
