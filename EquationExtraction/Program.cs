using Accord.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EquationExtraction
{
    class Program
    {
        static void Main(string[] args)
        {
            var metapath = ".\\..\\..\\..\\demo\\4.input.jpg.meta.json";

            var meta = Newtonsoft.Json.JsonConvert.DeserializeObject<OCR.Shared.ImageInfo>(File.ReadAllText(metapath));

            while (true)
            {
                var pos = Console.ReadLine().Split(' ').Select(p => int.Parse(p)).ToArray();
                var rect = new OCR.Shared.Rect { X = pos[0], Y = pos[1], W = pos[2], H = pos[3] };

                var filtered = meta.Words.Where(
                    p => 
                    p.GravityCenter.X >= rect.X && p.GravityCenter.X <= rect.X + rect.W && p.GravityCenter.Y >= rect.Y && p.GravityCenter.Y <= rect.Y + rect.H
                    || p.GeometryCenter.X >= rect.X && p.GeometryCenter.X <= rect.X + rect.W && p.GeometryCenter.Y >= rect.Y && p.GeometryCenter.Y <= rect.Y + rect.H).ToArray();

                var newrect = new OCR.Shared.Rect { X = filtered.Min(p => p.Position.X), Y = filtered.Min(p => p.Position.Y), W = 0, H = 0 };

                foreach (var word in filtered)
                {
                    var width = word.Position.X - newrect.X + word.Position.W;
                    var height = word.Position.Y - newrect.Y + word.Position.H;
                    if (newrect.W < width) newrect.W = width;
                    if (newrect.H < height) newrect.H = height;
                }

                var bytes = new byte[newrect.W * newrect.H];
                for (var i = 0; i < newrect.W * newrect.H; i++) bytes[i] = 0xFF;

                foreach (var word in filtered)
                {
                    var offsetx = word.Position.X - newrect.X;
                    var offsety = word.Position.Y - newrect.Y;

                    for (var j = 0; j < word.Position.H; j++)
                    {
                        for (var i = 0; i < word.Position.W; i++)
                        {
                            var d = word.Data[j * word.Position.W + i];
                            if (d == 0xFF) continue;

                            bytes[(offsety + j) * newrect.W + offsetx + i] = d;
                        }
                    }
                }

                var umbmp = UnmanagedImage.FromByteArray(bytes, newrect.W, newrect.H, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
                umbmp.ToManagedImage().Save(meta.FileName + "." + newrect.X + "." + newrect.Y + "." + newrect.W + "." + newrect.H + ".crop.jpg", System.Drawing.Imaging.ImageFormat.Png);
            }
        }
    }
}
