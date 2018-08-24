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
            var directory = ".";
            while (!Directory.Exists(Path.Combine(directory, "demo")))
            {
                directory = Path.Combine(directory, "..");
                if (!Directory.Exists(directory)) throw new DirectoryNotFoundException("cannot find demo directory");
            }

            foreach (var file in Directory.GetFiles(Path.Combine(directory, "demo"), "*.input.jpg"))
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

                var filters = new FiltersSequence(new BradleyLocalThresholding { WindowSize = 11, PixelBrightnessDifferenceLimit = 0.05f }, new Invert(), new BlobsFiltering(2, 2, max / 4, max / 4));
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

            var meta = new OCR.Shared.ImageInfo
            {
                FileName = fileName,
                MaskName = fileName + ".mask.png",
                FilteredName = fileName + ".filtered.png",
                Words = new List<OCR.Shared.Blob>(),
                Areas = new List<OCR.Shared.Rect>()
            };

            using (var img = System.Drawing.Image.FromFile(fileName))
            using (var bmp = new Bitmap(img))
            {
                var gray = new FiltersSequence(Grayscale.CommonAlgorithms.BT709, new GaussianSharpen(), new ContrastCorrection(20))
                    .Apply(UnmanagedImage.FromManagedImage(bmp));
                gray.ToManagedImage().Save(fileName + ".gray.png", System.Drawing.Imaging.ImageFormat.Png);

                var bw = new FiltersSequence(new BradleyLocalThresholding { PixelBrightnessDifferenceLimit = 0.20f }, new Invert())
                    .Apply(gray);
                bw.ToManagedImage().Save(fileName + ".bw.png", System.Drawing.Imaging.ImageFormat.Png);

                var mask = new FiltersSequence(new Dilation(), new BlobsFiltering(5, 5, gray.Width / 2, gray.Height / 2, false))
                    .Apply(bw);
                mask.ToManagedImage().Save(meta.MaskName, System.Drawing.Imaging.ImageFormat.Png);

                {
                    var ccs = new BlobCounter(mask).GetObjectsRectangles();
                    var sortedw = ccs.Select(p => p.Width).OrderBy(p => p).ToArray();
                    var sortedh = ccs.Select(p => p.Height).OrderBy(p => p).ToArray();

                    meta.MedianWordSize = new OCR.Shared.Point { X = sortedw[sortedw.Length / 2], Y = sortedh[sortedh.Length / 2] };

                    Console.WriteLine($"Median: {meta.MedianWordSize.X}, {meta.MedianWordSize.Y}");
                }

                var filtered = new FiltersSequence(new Invert(), new Intersect(mask), new Invert())
                    .Apply(gray);

                var filteredBmp = filtered.ToManagedImage();
                filteredBmp.Save(meta.FilteredName, System.Drawing.Imaging.ImageFormat.Png);

                var rgb = new GrayscaleToRGB().Apply(filtered);
                
                mask = new FiltersSequence(new HorizontalRunLengthSmoothing(meta.MedianWordSize.X * 2), new VerticalRunLengthSmoothing(meta.MedianWordSize.Y * 2))
                    .Apply(mask);
                mask.ToManagedImage().Save(fileName + ".area.png", System.Drawing.Imaging.ImageFormat.Png);

                foreach (Rectangle rect in new BlobCounter(mask).GetObjectsRectangles()) Drawing.FillRectangle(mask, rect, Color.White);
                mask.ToManagedImage().Save(fileName + ".rect.png", System.Drawing.Imaging.ImageFormat.Png);
                
                foreach (Rectangle rect in new BlobCounter(mask).GetObjectsRectangles())
                {
                    meta.Areas.Add(new OCR.Shared.Rect { X = rect.X, Y = rect.Y, W = rect.Width, H = rect.Height });
                    Drawing.Rectangle(rgb, rect, Color.Red);
                }

                new Intersect(mask).ApplyInPlace(bw);

                foreach (var blob in new BlobCounter(bw).GetObjects(bw, false))
                {
                    var outRect = new OCR.Shared.Rect { X = blob.Rectangle.X - 1, Y = blob.Rectangle.Y - 1, W = blob.Rectangle.Width + 2, H = blob.Rectangle.Height + 2 };
                    if (outRect.X < 0) { outRect.X = 0; outRect.W--; }
                    if (outRect.Y < 0) { outRect.Y = 0; outRect.H--; }
                    if (outRect.X + outRect.W > bw.Width) outRect.W = bw.Width - outRect.X;
                    if (outRect.Y + outRect.H > bw.Height) outRect.H = bw.Height - outRect.Y;

                    var gravityCenter = new OCR.Shared.Point { X = (int)blob.CenterOfGravity.X, Y = (int)blob.CenterOfGravity.Y };
                    var geometryCenter = new OCR.Shared.Point { X = blob.Rectangle.X + blob.Rectangle.Width / 2, Y = blob.Rectangle.Y + blob.Rectangle.Height / 2 };
                    
                    var bytedata = blob.Image.ToByteArray();
                    var newbytedata = new byte[outRect.W * outRect.H];
                    for (var j = 0; j < outRect.H - 2; j++) for (var i = 0; i < outRect.W - 2; i++) newbytedata[j * outRect.W + i + 1] = bytedata[j * (outRect.W - 2) + i];
                    var blobImg = new FiltersSequence(new Dilation()).Apply(UnmanagedImage.FromByteArray(newbytedata, outRect.W, outRect.H, System.Drawing.Imaging.PixelFormat.Format8bppIndexed));

                    var area = new Rectangle(outRect.X, outRect.Y, outRect.W, outRect.H);
                    Drawing.Rectangle(rgb, area, Color.Blue);

                    Drawing.FillRectangle(rgb, new Rectangle(geometryCenter.X - 2, geometryCenter.Y - 2, 5, 5), Color.Magenta);
                    Drawing.FillRectangle(rgb, new Rectangle(gravityCenter.X - 2, gravityCenter.Y - 2, 5, 5), Color.Cyan);

                    var bits = filteredBmp.LockBits(area, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

                    meta.Words.Add(
                        new OCR.Shared.Blob
                        {
                            Id = blob.ID,
                            Data = new FiltersSequence(new Invert(), new Intersect(blobImg), new Invert()).Apply(new UnmanagedImage(bits)).ToByteArray(),
                            Position = outRect,
                            GravityCenter = gravityCenter,
                            GeometryCenter = geometryCenter
                        });

                    filteredBmp.UnlockBits(bits);
                }



                rgb.ToManagedImage().Save(fileName + ".marked.png", System.Drawing.Imaging.ImageFormat.Png);

                File.WriteAllText(fileName + ".meta.json", Newtonsoft.Json.JsonConvert.SerializeObject(meta, Newtonsoft.Json.Formatting.Indented));
            }

        }

    }
}
