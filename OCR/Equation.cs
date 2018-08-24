using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Cloud.Vision.V1;

namespace OCR
{
    public class Equation
    {
        public string Content { get; set; }

        public List<Vertex> Vertices { get; set; }
    }
}
