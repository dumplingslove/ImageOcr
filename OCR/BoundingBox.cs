using Google.Cloud.Vision.V1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCR
{
    public class BoundingBox
    {
        public BoundingBox(List<Vertex> boundingPoly)
        {
            if (boundingPoly.Count != 4)
            {
                throw new ArgumentException(nameof(boundingPoly));
            }

            int xMax = 0;
            int xMin = int.MaxValue;
            int yMax = 0;
            int yMin = int.MaxValue;

            boundingPoly.ForEach(p =>
            {
                xMax = p.X > xMax ? p.X : xMax;
                xMin = p.X < xMin ? p.X : xMin;

                yMax = p.Y > yMax ? p.Y : yMax;
                yMin = p.Y < yMin ? p.Y : yMin;
            });

            this.OuterWidth = xMax - xMin;
            this.OuterHeight = yMax - yMin;
            this.Center = new Vertex()
            {
                X = (xMin + xMax) / 2,
                Y = (yMin + yMax) / 2,
            };

            var mostLeft = boundingPoly.Where(p => p.X == xMin).OrderBy(p => p.Y).ToList();
            var mostRight = boundingPoly.Where(p => p.X == xMax).OrderBy(p => p.Y).ToList();

            if (mostLeft.Count == 2)
            {
                var others = boundingPoly.Where(p => p.X != xMin).OrderBy(p => p.Y).ToList();

                this.UpLeftPoly = mostLeft[0];
                this.DownLeftPoly = mostLeft[1];
                this.UpRightPoly = others[0];
                this.DownRightPoly = others[1];
            }
            else if (mostRight.Count == 2)
            {
                var others = boundingPoly.Where(p => p.X != xMax).OrderBy(p => p.Y).ToList();

                this.UpLeftPoly = others[0];
                this.DownLeftPoly = others[1];
                this.UpRightPoly = mostRight[0];
                this.DownRightPoly = mostRight[1];
            }
            else if (mostLeft.Count == 1 && mostRight.Count == 1)
            {
                var others = boundingPoly.Where(p => p.X != xMin && p.X != xMax).OrderBy(p => p.Y).ToList();

                if (mostLeft[0].X < this.Center.X)
                {
                    this.UpLeftPoly = mostLeft[0];
                    this.DownLeftPoly = others[1];
                    this.UpRightPoly = others[0];
                    this.DownRightPoly = mostRight[0];
                }
                else
                {
                    this.UpLeftPoly = others[0];
                    this.DownLeftPoly = mostLeft[0];
                    this.UpRightPoly = mostRight[0];
                    this.DownRightPoly = others[1];
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public Vertex UpLeftPoly { get; set; }

        public Vertex UpRightPoly { get; set; }

        public Vertex DownRightPoly { get; set; }
        
        public Vertex DownLeftPoly { get; set; }

        public Vertex Center { get; set; }

        public int OuterWidth { get; set; }

        public int OuterHeight { get; set; }

        public static BoundingBox operator +(BoundingBox b1, BoundingBox b2)
        {
            var boundingPoly = new List<Vertex>();

            if (b1.Center.X < b2.Center.X)
            {
                boundingPoly.Add(b1.UpLeftPoly);
                boundingPoly.Add(b1.DownLeftPoly);
                boundingPoly.Add(b2.UpRightPoly);
                boundingPoly.Add(b2.DownRightPoly);
            }
            else
            {
                boundingPoly.Add(b2.UpLeftPoly);
                boundingPoly.Add(b2.DownLeftPoly);
                boundingPoly.Add(b1.UpRightPoly);
                boundingPoly.Add(b1.DownRightPoly);
            }
            
            return new BoundingBox(boundingPoly);
        }
    }
}
