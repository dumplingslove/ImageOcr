using Accord.Imaging;
using Google.Cloud.Vision.V1;
using Newtonsoft.Json;
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
        public static List<OcrResult.OcrLineResult> GenerateOcrLineResults(List<OcrResult.OcrSegmentResult> segments)
        {
            var result = new List<OcrResult.OcrLineResult>();
            var line = new OcrResult.OcrLineResult();
            line.Segments = new List<OcrResult.OcrSegmentResult>();
            OcrResult.OcrSegmentResult lastSegemnt = null;
            foreach (var segment in segments)
            {
                if (lastSegemnt != null 
                    && (lastSegemnt.Segment.BoundingBox.DownRightPoly.X > segment.Segment.BoundingBox.DownRightPoly.X
                    || lastSegemnt.Segment.BoundingBox.DownRightPoly.Y < segment.Segment.BoundingBox.UpLeftPoly.Y))
                {
                    result.Add(line);
                    line = new OcrResult.OcrLineResult();
                    line.Segments = new List<OcrResult.OcrSegmentResult>();
                    line.Segments.Add(segment);
                }
                else
                {
                    line.Segments.Add(segment);
                }

                lastSegemnt = segment;
            }

            result.Add(line);

            return result;
        }

        public static List<OcrResult.OcrSegmentResult> CombineRawOcrResult(List<Tuple<int, ChineseSegment>> pageWordList, List<Tuple<int, Equation>> pageEquationList)
        {
            var result = new List<OcrResult.OcrSegmentResult>();
            var wordIndex = 0;
            var equationIndex = 0;
            while (wordIndex < pageWordList.Count || equationIndex < pageEquationList.Count)
            {
                Tuple<int, ChineseSegment> currentWord = null;
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
                        result.Add(new OcrResult.OcrSegmentResult()
                        {
                            Index = currentWord.Item1,
                            Type = OcrResult.OcrSegmentResult.OcrResultType.ChineseSegment,
                            Segment = currentWord.Item2
                        });

                        wordIndex++;
                    }
                    else
                    {
                        result.Add(new OcrResult.OcrSegmentResult()
                        {
                            Index = currentEquation.Item1,
                            Type = OcrResult.OcrSegmentResult.OcrResultType.Equation,
                            Segment = currentEquation.Item2
                        });
                        
                        equationIndex++;
                    }
                }
                else if (currentWord != null)
                {
                    result.Add(new OcrResult.OcrSegmentResult()
                    {
                        Index = currentWord.Item1,
                        Type = OcrResult.OcrSegmentResult.OcrResultType.ChineseSegment,
                        Segment = currentWord.Item2
                    });

                    wordIndex++;
                }
                else if (currentEquation != null)
                {
                    result.Add(new OcrResult.OcrSegmentResult()
                    {
                        Index = currentEquation.Item1,
                        Type = OcrResult.OcrSegmentResult.OcrResultType.Equation,
                        Segment = currentEquation.Item2
                    });

                    equationIndex++;
                }
            }

            return result;
        }

        public static void OutputResultToJson(string filePath, List<OcrResult.OcrLineResult> lineResults)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath))
            {
                string output = JsonConvert.SerializeObject(lineResults);
                file.WriteLine(output);
            }
        }

        public static void OutputResultToHtml(string filePath, List<OcrResult.OcrLineResult> lineResults)
        {
            string htmlHeaderTemplate = @"<!DOCTYPE html><html><head><title>MathJax TeX Test Page</title><script type=""text/javascript"" async src=""https://cdn.mathjax.org/mathjax/latest/MathJax.js?config=TeX-AMS_CHTML""></script></head><body>{0}</body></html>";
            string htmlLineTemplate = @"<span>{0}</span><br>";

            var content = new StringBuilder();
            foreach (var line in lineResults)
            {
                var lineContent = new StringBuilder();
                foreach (var segemnt in line.Segments)
                {
                    lineContent.Append(segemnt.Segment.Content);
                }

                var htmlLine = string.Format(htmlLineTemplate, lineContent.ToString());
                content.Append(htmlLine);
            }

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath))
            {
                file.WriteLine(string.Format(htmlHeaderTemplate, content.ToString()));
            }
        }

        public static void OutputResultToFile(string filePath, List<OcrResult.OcrLineResult> lineResults)
        {
            // Output the OCR result
            var dateString = DateTime.Now.ToString("yyyy-mm-dd-ss");
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath))
            {

                foreach (var line in lineResults)
                {
                    foreach (var segemnt in line.Segments)
                    {
                        var vertices = new List<Vertex>();
                        vertices.Add(segemnt.Segment.BoundingBox.UpLeftPoly);
                        vertices.Add(segemnt.Segment.BoundingBox.UpRightPoly);
                        vertices.Add(segemnt.Segment.BoundingBox.DownRightPoly);
                        vertices.Add(segemnt.Segment.BoundingBox.DownLeftPoly);
                        var box = string.Join(" - ", vertices.Select(v => $"({v.X}, {v.Y})"));
                        file.WriteLine(segemnt.Index + " " + segemnt.Segment.Content + " " + segemnt.Segment.BoundingBox.OuterWidth + " " + box);
                    }
                    file.WriteLine();
                }
            }
        }

        public static void OutputEquationsToImage(string imageFolder, string originalImagefile, List<OcrResult.OcrSegmentResult> rawResults)
        {
            Bitmap originalImage = new Bitmap(originalImagefile);

            // Output the OCR result
            foreach (var rawResult in rawResults)
            {
                if (rawResult.Type == OcrResult.OcrSegmentResult.OcrResultType.Equation)
                {
                    var upLeft = rawResult.Segment.BoundingBox.UpLeftPoly;
                    var downRight = rawResult.Segment.BoundingBox.DownRightPoly;
                    var upX = upLeft.X < 0 ? 0 : upLeft.X;
                    var upY = upLeft.Y < 0 ? 0 : upLeft.Y;

                    // Has some offset by API result
                    // upX += 6;
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

        public static List<Tuple<int, Equation>> MergeEquations(List<Tuple<int, Equation>> paragraphEquationList)
        {
            var result = new List<Tuple<int, Equation>>();

            // Merge the continue single equations
            if (paragraphEquationList.Count < 2)
            {
                result.AddRange(paragraphEquationList);
            }
            else
            {
                var combinedParagraphEquationList = new List<Tuple<int, Equation>>();

                var tempEquation = paragraphEquationList.Last();
                for (int i = paragraphEquationList.Count - 2; i >= 0; i--)
                {
                    if (paragraphEquationList[i].Item1 == tempEquation.Item1 - 1
                        && paragraphEquationList[i].Item2.BoundingBox.DownLeftPoly.X < tempEquation.Item2.BoundingBox.DownLeftPoly.X)
                    {
                        tempEquation = new Tuple<int, Equation>(paragraphEquationList[i].Item1, new Equation()
                        {
                            Index = paragraphEquationList[i].Item1,
                            Content = paragraphEquationList[i].Item2.Content + tempEquation.Item2.Content,
                            BoundingBox = paragraphEquationList[i].Item2.BoundingBox + tempEquation.Item2.BoundingBox,
                        });
                    }
                    else
                    {
                        combinedParagraphEquationList.Add(tempEquation);
                        tempEquation = paragraphEquationList[i];
                    }
                }

                combinedParagraphEquationList.Add(tempEquation);
                combinedParagraphEquationList.Reverse();

                result.AddRange(combinedParagraphEquationList);
            }

            return result;
        }

        public static List<Tuple<int, ChineseSegment>> MergeChineseSegments(List<Tuple<int, ChineseSegment>> paragraphChineseSegmentList)
        {
            var result = new List<Tuple<int, ChineseSegment>>();

            // Merge the continue single ChineseSegments
            if (paragraphChineseSegmentList.Count < 2)
            {
                result.AddRange(paragraphChineseSegmentList);
            }
            else
            {
                var combinedParagraphChineseSegmentList = new List<Tuple<int, ChineseSegment>>();

                var tempChineseSegment = paragraphChineseSegmentList.Last();
                for (int i = paragraphChineseSegmentList.Count - 2; i >= 0; i--)
                {
                    if (paragraphChineseSegmentList[i].Item1 == tempChineseSegment.Item1 - 1
                        && paragraphChineseSegmentList[i].Item2.BoundingBox.DownRightPoly.Y > tempChineseSegment.Item2.BoundingBox.UpLeftPoly.Y)
                    {
                        tempChineseSegment = new Tuple<int, ChineseSegment>(paragraphChineseSegmentList[i].Item1, new ChineseSegment()
                        {
                            Index = paragraphChineseSegmentList[i].Item1,
                            Content = paragraphChineseSegmentList[i].Item2.Content + tempChineseSegment.Item2.Content,
                            BoundingBox = paragraphChineseSegmentList[i].Item2.BoundingBox + tempChineseSegment.Item2.BoundingBox,
                        });
                    }
                    else
                    {
                        combinedParagraphChineseSegmentList.Add(tempChineseSegment);
                        tempChineseSegment = paragraphChineseSegmentList[i];
                    }
                }

                combinedParagraphChineseSegmentList.Add(tempChineseSegment);
                combinedParagraphChineseSegmentList.Reverse();

                result.AddRange(combinedParagraphChineseSegmentList);
            }

            return result;
        }
    }
}
