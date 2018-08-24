using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCR.Shared
{
    public class ImageInfo
    {
        public string FileName { get; set; }
        public string MaskName { get; set; }
        public string FilteredName { get; set; }
        public List<Blob> Words { get; set; }
        public List<Rect> Areas { get; set; }
        public Point MedianWordSize { get; set; }
    }

    public class Blob
    {
        public byte[] Data { get; set; }
        public Rect Position { get; set; }
        public Point GravityCenter { get; set; }
        public Point GeometryCenter { get; set; }
    }

    public struct Rect
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }
    }
    
    public struct Point
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
}
