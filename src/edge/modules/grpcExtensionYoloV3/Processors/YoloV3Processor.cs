// -----------------------------------------------------------------------
//  <copyright company="Microsoft Corporation">
//      Copyright (C) Microsoft Corporation. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using NumSharp;
using Microsoft.Extensions.Logging;

namespace grpcExtension.Processors
{
    public class YoloV3Processor
    {
        private readonly ILogger _logger;
        // Input in NCHW floats (1 array, 3 channels, 416 pixels by 416 pixels).
        private const int ModelInputs = 1;
        private const int ModelChannels = 3;
        private const int ModelWidth = 416;
        private const int ModelHeigth = 416;
        private readonly SessionOptions _sessionOptions;
        private readonly InferenceSession _inferenceSession;

        public YoloV3Processor(string modelPath, ILogger logger)
        {
            _logger = logger;

            _sessionOptions = new SessionOptions
            {
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
                ExecutionMode = ExecutionMode.ORT_PARALLEL,
                InterOpNumThreads = Environment.ProcessorCount * 2,
            };

            _inferenceSession = new InferenceSession(modelPath, _sessionOptions);
        }
        /// <inheritdoc />
        public List<ORTItem> Run(Bitmap bitmap)
        {
            try
            {


                float[] imgData = LoadTensorFromImageFile(bitmap);
                int h = bitmap.Height;
                int w = bitmap.Width;

                var container = new List<NamedOnnxValue>();
                //yolov3 customization
                var tensor1 = new DenseTensor<float>(imgData, new int[] { ModelInputs, ModelChannels, ModelWidth, ModelHeigth });
                container.Add(NamedOnnxValue.CreateFromTensor<float>("input_1", tensor1));
                var tensor2 = new DenseTensor<float>(new float[] { h, w }, new int[] { 1, 2 });
                container.Add(NamedOnnxValue.CreateFromTensor<float>("image_shape", tensor2));

                // Run the inference
                using var results = _inferenceSession.Run(container);
                List<ORTItem> itemList = PostProcessing(results);
                return itemList;
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Exception running inference: {ex.Message}.");
                throw ex;
            }
        }

        static List<ORTItem> PostProcessing(IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results)
        {
            List<ORTItem> itemList = new List<ORTItem>();

            List<float[]> out_boxes = new List<float[]>();
            List<float[]> out_scores = new List<float[]>();
            List<int> out_classes = new List<int>();

            var boxes = results.AsEnumerable().ElementAt(0).AsTensor<float>();
            var scores = results.AsEnumerable().ElementAt(1).AsTensor<float>();
            var indices = results.AsEnumerable().ElementAt(2).AsTensor<int>();
            int nbox = indices.Count() / 3;

            for (int ibox = 0; ibox < nbox; ibox++)
            {
                out_classes.Add(indices[0, 0, ibox * 3 + 1]);

                float[] score = new float[80];
                for (int j = 0; j < 80; j++)
                {
                    score[j] = scores[indices[0, 0, ibox * 3 + 0], j, indices[0, 0, ibox * 3 + 2]];
                }
                out_scores.Add(score);

                float[] box = new float[]
                {
                    boxes[indices[0, 0, ibox * 3 + 0], indices[0, 0, ibox * 3 + 2], 0],
                    boxes[indices[0, 0, ibox * 3 + 0], indices[0, 0, ibox * 3 + 2], 1],
                    boxes[indices[0, 0, ibox * 3 + 0], indices[0, 0, ibox * 3 + 2], 2],
                    boxes[indices[0, 0, ibox * 3 + 0], indices[0, 0, ibox * 3 + 2], 3]
                };
                out_boxes.Add(box);

                //output
                ORTItem item = new ORTItem((int)box[1], (int)box[0], (int)(box[3] - box[1]), (int)(box[2] - box[0]), out_classes[ibox], Yolov3BaseConfig.Labels[out_classes[ibox]], out_scores[ibox][out_classes[ibox]]);
                itemList.Add(item);
            }

            return itemList;
        }

        static float[] LoadTensorFromImageFile(Bitmap bitmap)
        {
            RGBtoBGR(bitmap);
            int iw = bitmap.Width, ih = bitmap.Height, w = 416, h = 416, nw, nh;

            float scale = Math.Min((float)w / iw, (float)h / ih);
            nw = (int)(iw * scale);
            nh = (int)(ih * scale);

            //resize
            Bitmap rsImg = ResizeImage(bitmap, nw, nh);
            Bitmap boxedImg = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            using (Graphics gr = Graphics.FromImage(boxedImg))
            {
                gr.FillRectangle(new SolidBrush(Color.FromArgb(255, 128, 128, 128)), 0, 0, boxedImg.Width, boxedImg.Height);
                gr.DrawImage(rsImg, new Point((int)((w - nw) / 2), (int)((h - nh) / 2)));
            }
            var imgData = boxedImg.ToNDArray(flat: false, copy: true);
            imgData /= 255.0;
            imgData = np.transpose(imgData, new int[] { 0, 3, 1, 2 });
            imgData = imgData.reshape(1, 3 * w * h);

            double[] doubleArray = imgData[0].ToArray<double>();
            float[] floatArray = new float[doubleArray.Length];
            for (int i = 0; i < doubleArray.Length; i++)
            {
                floatArray[i] = (float)doubleArray[i];
            }

            return floatArray;
        }

        private static Bitmap ResizeImage(Bitmap image, int width, int height)
        {
            var destRect = new System.Drawing.Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(96, 96);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using var wrapMode = new ImageAttributes();
                wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
            }

            return destImage;
        }

        private static void RGBtoBGR(Bitmap bmp)
        {
            BitmapData data = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, bmp.PixelFormat);

            int length = Math.Abs(data.Stride) * bmp.Height;

            byte[] imageBytes = new byte[length];
            IntPtr scan0 = data.Scan0;
            Marshal.Copy(scan0, imageBytes, 0, imageBytes.Length);

            byte[] rgbValues = new byte[length];
            for (int i = 0; i < length; i += 3)
            {
                rgbValues[i] = imageBytes[i + 2];
                rgbValues[i + 1] = imageBytes[i + 1];
                rgbValues[i + 2] = imageBytes[i];
            }
            Marshal.Copy(rgbValues, 0, scan0, length);

            bmp.UnlockBits(data);
        }
    }
}