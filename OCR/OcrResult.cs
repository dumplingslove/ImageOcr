using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCR
{
    public class OcrResult
    {
        public class OcrSegmentResult
        {
            public enum OcrResultType
            {
                ChineseSegment,
                Equation,
            }

            public int Index { get; set; }

            public OcrResultType Type { get; set; }

            public Segment Segment { get; set; }
        }

        public class OcrLineResult
        {
            public List<OcrSegmentResult> Segments { get; set; }
        }
    }
}
