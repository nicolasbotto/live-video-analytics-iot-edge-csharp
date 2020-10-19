using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace grpcExtension.Models
{
    public class Classification
    {
        public double Confidence { get; set; }
        public string Value { get; set; }
    }
}
