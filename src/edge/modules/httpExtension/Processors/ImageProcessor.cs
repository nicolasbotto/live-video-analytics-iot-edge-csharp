// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using httpExtension.Models;
using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace httpExtension.Processors
{
    public class ImageProcessor
    {
        private readonly ILogger logger;
        public ImageProcessor(ILogger logger)
        {
            this.logger = logger;
        }

        public Inference ProcessImage(Image image)
        {
            var grayScaleImage = ToGrayScale(image);

            byte[] imageBytes = GetBytes(grayScaleImage);

            var totalColor = imageBytes.Sum(x => x);

            double avgColor = totalColor / imageBytes.Length;
            string colorIntensity = avgColor < 127 ? "dark" : "light";

            logger.LogInformation($"Average color = {avgColor}");

            var inference = new Inference
            {
                Type = "classification",
                SubType = "colorIntensity",
                Classification = new Classification()
                {
                    Confidence = 1.0,
                    Value = colorIntensity
                }
            };

            return inference;
        }

        private Bitmap ToGrayScale(Image source)
        {
            Bitmap grayscaleBitmap = new Bitmap(source.Width, source.Height);

            using Graphics graphics = Graphics.FromImage(grayscaleBitmap);

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
            graphics.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height),
                0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);

            return grayscaleBitmap;
        }

        private byte[] GetBytes(Bitmap image)
        {
            MemoryStream stream = new MemoryStream();
            image.Save(stream, ImageFormat.Jpeg);
            return stream.ToArray();
        }
    }
}
