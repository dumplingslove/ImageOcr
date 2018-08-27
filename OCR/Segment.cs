using System;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Cloud.Vision.V1;

namespace OCR
{
    public class Segment
    {
        public int Index { get; set; }

        public string Content { get; set; }

        public BoundingBox BoundingBox { get; set; }
    }
}
