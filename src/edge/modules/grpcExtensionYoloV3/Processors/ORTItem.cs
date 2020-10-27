using System;
using System.Collections.Generic;
using System.Text;

namespace grpcExtension.Processors
{
    public class ORTItem
    {
        public string ObjName { get; set; }
        public double Confidence { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int ObjId { get; set; }

        public ORTItem(int x, int y, int width, int height, int catId, string catName, double confidence)
        {
            this.X = Math.Max(0, x);
            this.Y = Math.Max(0, y);
            this.Width = width;
            this.Height = height;
            this.ObjId = catId;
            this.ObjName = catName;
            this.Confidence = confidence;
        }
    }
}
