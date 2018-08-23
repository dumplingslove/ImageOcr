using Accord.Imaging;
using Accord.Imaging.Filters;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Whiteboard
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (var file in Directory.GetFiles(@"D:\workspace\ocr\data", "*.input.jpg"))
            {
                Process2(file);
            }

        }

        static void Process(string fileName)
        {
            var mapping = new byte[] 
            {
                0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 6, 6, 6, 7, 7, 7, 8, 8, 9, 9,
                10, 10, 11, 11, 12, 12, 13, 13, 14, 15, 15, 16, 17, 17, 18, 19, 19, 20, 21, 22, 23, 23, 24, 25, 26, 27, 28, 29, 30,
                31, 32, 33, 34, 36, 37, 38, 39, 40, 42, 43, 44, 45, 47, 48, 50, 51, 52, 54, 55, 57, 58, 60, 61, 63, 65, 66, 68, 70,
                71, 73, 75, 77, 78, 80, 82, 84, 86, 87, 89, 91, 93, 95, 97, 99, 101, 103, 105, 107, 109, 110, 112, 114, 116, 118, 120,
                122, 124, 127, 129, 131, 133, 135, 137, 139, 141, 143, 145, 146, 148, 150, 152, 154, 156, 158, 160,
                162, 164, 166, 168, 169, 171, 173, 175, 177, 178, 180, 182, 184, 185, 187, 189, 190, 192, 194, 195, 197, 198, 200,
                201, 203, 204, 205, 207, 208, 210, 211, 212, 213, 215, 216, 217, 218, 219, 221, 222, 223, 224, 225, 226, 227, 228, 229, 230,
                231, 232, 232, 233, 234, 235, 236, 236, 237, 238, 238, 239, 240,
                240, 241, 242, 242, 243, 243, 244, 244, 245, 245, 246, 246, 247, 247, 248, 248, 248, 249, 249, 249, 250, 250, 250,
                251, 251, 251, 251, 252, 252, 252, 252, 253, 253, 253, 253, 253, 254, 254, 254, 254, 254, 254, 255, 255, 255, 255, 255, 255, 255, 255
            };

            Console.WriteLine(fileName);

            using (var img = System.Drawing.Image.FromFile(fileName))
            using (var bmp = new Bitmap(img))
            {
                var umbmp = UnmanagedImage.FromManagedImage(bmp);
                //new ConservativeSmoothing(11).ApplyInPlace(umbmp);
                //umbmp.ToManagedImage().Save(fileName + ".smooth.png", System.Drawing.Imaging.ImageFormat.Png);

                var gray = Grayscale.CommonAlgorithms.BT709.Apply(umbmp);

                var max = 360;

                var width = gray.Width;
                var height = gray.Height;

                if (width > max || height > max)
                {
                    if (width > height)
                    {
                        height = height * max / width;
                        width = max;
                    }
                    else
                    {
                        width = width * max / height;
                        height = max;
                    }
                }

                var thumbnail = new ResizeBicubic(width, height).Apply(gray);
                thumbnail.ToManagedImage().Save(fileName + ".resize.png", System.Drawing.Imaging.ImageFormat.Png);

                var filters = new FiltersSequence(/*new BilateralSmoothing { KernelSize = 7 }, */new BradleyLocalThresholding { WindowSize = 11, PixelBrightnessDifferenceLimit = 0.05f }, new Invert(), new BlobsFiltering(2, 2, max/4, max/4));
                thumbnail = filters.Apply(thumbnail);

                

                //var qf = new QuadrilateralFinder();
                //var points = qf.ProcessImage(gray);

                //Drawing.Polygon(gray, points, Color.White);
                //for (int i = 0; i < points.Count; i++)
                //{
                //    Drawing.FillRectangle(gray,
                //        new Rectangle(points[i].X - 2, points[i].Y - 2, 5, 5),
                //        Color.White);
                //}

                thumbnail.ToManagedImage().Save(fileName + ".skeleton.png", System.Drawing.Imaging.ImageFormat.Png);

                var statistics = new ImageStatistics(gray);
                
                var whiteTarget = (int)(statistics.Gray.TotalCount * 0.3);
                var blackTarget = (int)(statistics.Gray.TotalCount * 0.01);

                int gw = -1;
                int gb = -1;
                //int rw = -1, gw = -1, bw = -1;
                //int rb = -1, gb = -1, bb = -1;

                //for (int i = 0, rc = 0, gc = 0, bc = 0; i < 256; i++)
                for (int i = 0, gc = 0; i < 256; i++)
                {
                    gw = gw == -1 && (gc += statistics.Gray.Values[i]) >= whiteTarget ? i : gw;
                    //rw = rw == -1 && (rc += statistics.Red.Values[i]) >= whiteTarget ? i : rw;
                    //gw = gw == -1 && (gc += statistics.Green.Values[i]) >= whiteTarget ? i : gw;
                    //bw = bw == -1 && (bc += statistics.Blue.Values[i]) >= whiteTarget ? i : bw;

                    gb = gb == -1 && (gc += statistics.Gray.Values[i]) >= blackTarget ? i : gb;
                    //rb = rb == -1 && (rc += statistics.Red.Values[i]) >= blackTarget ? i : rb;
                    //gb = gb == -1 && (gc += statistics.Green.Values[i]) >= blackTarget ? i : gb;
                    //bb = bb == -1 && (bc += statistics.Blue.Values[i]) >= blackTarget ? i : bb;
                }

                var gp = new byte[256];
                //var rp = new byte[256];
                //var gp = new byte[256];
                //var bp = new byte[256];

                for (var i = 0; i < 256; i++)
                {
                    gp[i] = i < gb ? (byte)0 : i < gw ? mapping[(i - gb) * 256 / (gw - gb)] : (byte)255;
                    //rp[i] = i < rb ? (byte)0 : i < rw ? mapping[(i - rb) * 256 / (rw - rb)] : (byte)255;
                    //gp[i] = i < gb ? (byte)0 : i < gw ? mapping[(i - gb) * 256 / (gw - gb)] : (byte)255;
                    //bp[i] = i < bb ? (byte)0 : i < bw ? mapping[(i - bb) * 256 / (bw - bb)] : (byte)255;
                }

                //new ColorRemapping(rp, gp, bp).ApplyInPlace(umbmp);
                new ColorRemapping(gp).ApplyInPlace(gray);

                gray.ToManagedImage().Save(fileName + ".pure.png", System.Drawing.Imaging.ImageFormat.Png);

                //QuadrilateralTransformation filter = new QuadrilateralTransformation(new List<Accord.IntPoint>(new Accord.IntPoint[] { new Accord.IntPoint(489, 269), new Accord.IntPoint(3561, 101), new Accord.IntPoint(3657, 2889), new Accord.IntPoint(261, 2613) }))
                //{
                //    AutomaticSizeCalculaton = true,
                //    UseInterpolation = true
                //};

                //umbmp = filter.Apply(umbmp);

                //umbmp.ToManagedImage().Save(fileName + ".result.png", System.Drawing.Imaging.ImageFormat.Png);

            }

        }

        static void Process2(string fileName)
        {
            Console.WriteLine(fileName);

            using (var img = System.Drawing.Image.FromFile(fileName))
            using (var bmp = new Bitmap(img))
            {
                var umbmp = UnmanagedImage.FromManagedImage(bmp);
                var gray = Grayscale.CommonAlgorithms.BT709.Apply(umbmp);
                gray.ToManagedImage().Save(fileName + ".gray.png", System.Drawing.Imaging.ImageFormat.Png);

                var max = (gray.Width + gray.Height) / 2 / 2;
                var filters = new FiltersSequence(new BilateralSmoothing { KernelSize = 7 }, new BradleyLocalThresholding { PixelBrightnessDifferenceLimit = 0.10f }, new Invert(), new BlobsFiltering(3, 3, max, max) { CoupledSizeFiltering = true });
                var bw = filters.Apply(gray);
                bw.ToManagedImage().Save(fileName + ".bw.png", System.Drawing.Imaging.ImageFormat.Png);

                filters = new FiltersSequence(new Invert(), new Intersect(bw), new Invert());
                gray = filters.Apply(gray);

                gray.ToManagedImage().Save(fileName + ".filtered.png", System.Drawing.Imaging.ImageFormat.Png);

            }

        }

    }
}
