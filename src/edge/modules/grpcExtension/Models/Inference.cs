// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace grpcExtension.Models
{
    public class Inference
    {
        public string Type { get; set; }

        public string SubType { get; set; }
        public Classification Classification { get; set; }
    }
}
