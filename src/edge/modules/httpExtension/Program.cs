using System;
using System.IO;
using System.Text;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LVAExtension_Http
{
    class Program
    {

        static void Main(string[] args)
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://*:8080/score/");
            listener.Start();
            Console.WriteLine("Listening...");
            for(;;)
            {
                HttpListenerContext ctx = listener.GetContext();
                new Thread(new Worker(ctx).ProcessRequest).Start();
            }

        }

        class Worker
        {
            private HttpListenerContext context;

            public Worker(HttpListenerContext ctx)
            {
                this.context = ctx;
            }

            public void ProcessRequest()
            {
                Console.WriteLine("Processing Request... " + context.Request.HttpMethod + " " + context.Request.Url);
                
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = 204;

                string inference = "";

                if (context.Request.ContentLength64 > 0)
                {
                    using (var requestStream = context.Request.InputStream)
                    {
                        var image = Image.FromStream(requestStream);
                        inference = ProcessImage(image);
                    }
                }
                
                //string msg = "{\"msg\" : \"Hello World\"}";
                if (inference.Length > 0)
                {
                    context.Response.StatusCode = 200;
                }

                byte[] b = Encoding.UTF8.GetBytes(inference);
                context.Response.ContentLength64 = b.Length;
                context.Response.OutputStream.Write(b, 0, b.Length);
                context.Response.OutputStream.Close();
            }

            string ProcessImage(Image image)
            {
                string inferenceString = "";
                using (var grayScaleImage = ToGrayScale(image))
                {
                    byte[] imageBytes = GetBytes(grayScaleImage);
                    double totalColor = 0;
                    for(int i = 0; i < imageBytes.Length; i++)
                    {
                        totalColor += imageBytes[i];
                    }

                    double avgColor = totalColor / imageBytes.Length;
                    string colorIntensity = "light";
                    if (avgColor < 127)
                    {
                        colorIntensity = "dark";
                    }

                    Console.WriteLine("Average color = " + avgColor.ToString());

                    dynamic inference = new JObject();
                    inference.type = "classification";
                    inference.subtype = "colorIntesity";

                    dynamic classification = new JObject();
                    classification.value = colorIntensity;
                    classification.confidence = 1.0;
                    
                    inference.classification = classification;

                    inferenceString = inference.ToString();                    
                }

                return inferenceString;
            }

            private Bitmap ToGrayScale(Image source)
            {
                Bitmap grayscaleBitmap = new Bitmap(source.Width, source.Height);
                
                Graphics g = Graphics.FromImage(grayscaleBitmap);

                //create the grayscale ColorMatrix
                ColorMatrix colorMatrix = new ColorMatrix(
                    new float[][]
                    {
                        new float[] {.3f, .3f, .3f, 0, 0},
                        new float[] {.59f, .59f, .59f, 0, 0},
                        new float[] {.11f, .11f, .11f, 0, 0},
                        new float[] {0, 0, 0, 1, 0},
                        new float[] {0, 0, 0, 0, 1}
                    });

                ImageAttributes attributes = new ImageAttributes();

                //set the color matrix attribute
                attributes.SetColorMatrix(colorMatrix);

                //draw the original image on the new image
                //using the grayscale color matrix
                g.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height),
                    0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);

                //dispose the Graphics object
                g.Dispose();
                return grayscaleBitmap;
            }                    

            private byte[] GetBytes(Bitmap image)
            {
                //Check.NotNull(image, nameof(image));

                var region = new Rectangle(0, 0, image.Width, image.Height);

                var bitmapData = image.LockBits(region, ImageLockMode.ReadOnly, image.PixelFormat);

                var ptr = bitmapData.Scan0;
                var length = Math.Abs(bitmapData.Stride) * image.Height;

                var rgbValues = new byte[length];

                Marshal.Copy(ptr, rgbValues, 0, length);

                image.UnlockBits(bitmapData);

                return rgbValues;
            }            


        }

    }
}
