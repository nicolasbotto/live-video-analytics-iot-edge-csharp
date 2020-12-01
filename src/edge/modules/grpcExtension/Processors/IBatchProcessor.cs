using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace GrpcExtension.Processors
{
    public interface IBatchProcessor
    {
        IEnumerable<Inference> ProcessImages(List<Image> images);
        bool IsMediaFormatSupported(MediaDescriptor mediaDescriptor, out string errorMessage);
    }
}
