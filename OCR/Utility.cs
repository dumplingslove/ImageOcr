using Accord.Imaging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCR
{
    public static class Utility
    {
        public static List<OcrResult> CombineRawOcrResult(List<Tuple<int, string>> pageWordList, List<Tuple<int, Equation>> pageEquationList)
        {
            var result = new List<OcrResult>();
            var wordIndex = 0;
            var equationIndex = 0;
            while (wordIndex < pageWordList.Count || equationIndex < pageEquationList.Count)
            {
                Tuple<int, string> currentWord = null;
                Tuple<int, Equation> currentEquation = null;
                if (wordIndex < pageWordList.Count)
                {
                    currentWord = pageWordList[wordIndex];
                }

                if (equationIndex < pageEquationList.Count)
                {
                    currentEquation = pageEquationList[equationIndex];
                }

                if (currentWord != null && currentEquation != null)
                {
                    if (currentWord.Item1 < currentEquation.Item1)
                    {
                        result.Add(new OcrResult()
                        {
                            Index = currentWord.Item1,
                            Type = OcrResult.OcrResultType.ChineseSegment,
                            ChineseSegment = currentWord.Item2
                        });

                        wordIndex++;
                    }
                    else
                    {
                        result.Add(new OcrResult()
                        {
                            Index = currentEquation.Item1,
                            Type = OcrResult.OcrResultType.Equation,
                            Equation = currentEquation.Item2
                        });
                        
                        equationIndex++;
                    }
                }
                else if (currentWord != null)
                {
                    result.Add(new OcrResult()
                    {
                        Index = currentWord.Item1,
                        Type = OcrResult.OcrResultType.ChineseSegment,
                        ChineseSegment = currentWord.Item2
                    });

                    wordIndex++;
                }
                else if (currentEquation != null)
                {
                    result.Add(new OcrResult()
                    {
                        Index = currentEquation.Item1,
                        Type = OcrResult.OcrResultType.Equation,
                        Equation = currentEquation.Item2
                    });

                    equationIndex++;
                }
            }

            return result;
        }

        public static void OutputResultToFile(string filePath, List<OcrResult> rawResults)
        {
            // Output the OCR result
            var dateString = DateTime.Now.ToString("yyyy-mm-dd-ss");
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath))
            {
                foreach(var rawResult in rawResults)
                {
                    if (rawResult.Type == OcrResult.OcrResultType.ChineseSegment)
                    {
                        file.WriteLine(rawResult.Index + " " + rawResult.ChineseSegment);
                    }
                    else if (rawResult.Type == OcrResult.OcrResultType.Equation)
                    {
                        var box = string.Join(" - ", rawResult.Equation.Vertices.Select(v => $"({v.X}, {v.Y})"));
                        file.WriteLine(rawResult.Index + " " + rawResult.Equation.Content + " " + box);
                    }
                }
            }
        }

        public static void OutputEquationsToImage(string imageFolder, string originalImagefile, List<OcrResult> rawResults)
        {
            Bitmap originalImage = new Bitmap(originalImagefile);

            // Output the OCR result
            foreach (var rawResult in rawResults)
            {
                if (rawResult.Type == OcrResult.OcrResultType.Equation)
                {
                    var upLeft = rawResult.Equation.Vertices[0];
                    var downRight = rawResult.Equation.Vertices[2];
                    var upX = upLeft.X < 0 ? 0 : upLeft.X;
                    var upY = upLeft.Y < 0 ? 0 : upLeft.Y;

                    // Has some offset by API result
                    upX += 6;
                    BitmapData equationImageData = originalImage.LockBits(
                        new Rectangle(upX, upY, downRight.X - upLeft.X, downRight.Y - upLeft.Y),
                        ImageLockMode.ReadWrite, originalImage.PixelFormat);

                    try
                    {
                        UnmanagedImage equationData = new UnmanagedImage(equationImageData);
                        var equationImage = equationData.ToManagedImage();
                        var equationImagePath = Path.Combine(imageFolder, Path.GetFileName(originalImagefile) + "_eq_" + rawResult.Index + ".jpeg");
                        equationImage.Save(equationImagePath, ImageFormat.Jpeg);
                    }
                    finally
                    {
                        originalImage.UnlockBits(equationImageData);
                    }
                }
            }
        }
    }
}
