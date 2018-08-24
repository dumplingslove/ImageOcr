using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCR
{
    public class OcrResult
    {
        public enum OcrResultType
        {
            ChineseSegment,
            Equation,
        }

        public int Index { get; set; }

        public OcrResultType Type { get; set; }

        public string ChineseSegment { get; set; }

        public Equation Equation { get; set; }
    }
}
