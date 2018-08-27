using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Google.Cloud.Vision.V1;

namespace OCR
{
    public class GoogleOcr
    {
        public ImageAnnotatorClient Client { get; set; }

        public GoogleOcr()
        {
            // Instantiates a client
            Client = ImageAnnotatorClient.Create();
        }

        public void QuestionOcr(Image image, out List<Tuple<int, ChineseSegment>> pageWordList, out List<Tuple<int, Equation>> pageEquationList)
        {
            // Call API do the text detection
            TextAnnotation text = this.Client.DetectDocumentText(image);
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine($"Text: {text.Text}");

            pageWordList = new List<Tuple<int, ChineseSegment>>();
            pageEquationList = new List<Tuple<int, Equation>>();
            var index = 0;
            double avgChineseWidth = 0;
            double totalChinese = 0;
            double totalChineseWidth = 0;
            foreach (var page in text.Pages)
            {
                foreach (var block in page.Blocks)
                {
                    foreach (var paragraph in block.Paragraphs)
                    {
                        foreach (var word in paragraph.Words)
                        {
                            foreach (var symbol in word.Symbols)
                            {
                                if (Helper.IsChinese(symbol.Text[0]))
                                {
                                    var box = new BoundingBox(symbol.BoundingBox.Vertices.ToList());
                                    totalChinese += 1;
                                    totalChineseWidth += box.OuterWidth;
                                }
                            }
                        }
                    }
                }
            }

            avgChineseWidth = totalChineseWidth / totalChinese;
            double gapWidth = avgChineseWidth * 0.7;

            foreach (var page in text.Pages)
            {
                foreach (var block in page.Blocks)
                {
                    string box = string.Join(" - ", block.BoundingBox.Vertices.Select(v => $"({v.X}, {v.Y})"));
                    Console.WriteLine($"Block {block.BlockType} at {box}");
                    foreach (var paragraph in block.Paragraphs)
                    {
                        box = string.Join(" - ", paragraph.BoundingBox.Vertices.Select(v => $"({v.X}, {v.Y})"));
                        Console.WriteLine($"  Paragraph at {box}");

                        var paragraphWordList = new List<Tuple<int, ChineseSegment>>();
                        var paragraphEquationList = new List<Tuple<int, Equation>>();

                        var tempWord = new StringBuilder();
                        BoundingBox lastSymbolBoundingBox = null;
                        Symbol lastSymbol = null;
                        foreach (var word in paragraph.Words)
                        {
                            Console.WriteLine($"    Word: {string.Join("", word.Symbols.Select(s => s.Text))}");

                            foreach (var symbol in word.Symbols)
                            {
                                var boundingBox = new BoundingBox(symbol.BoundingBox.Vertices.ToList());
                                box = string.Join(" - ", symbol.BoundingBox.Vertices.Select(v => $"({v.X}, {v.Y})"));
                                Console.WriteLine($"      Symbol at {box}");
                                Console.WriteLine($"        Symbol: {symbol.Text}");

                                // Check whether the gap between last symbol is big enough to treat as equation
                                if (lastSymbolBoundingBox != null && lastSymbolBoundingBox.DownRightPoly.X < boundingBox.DownLeftPoly.X)
                                {
                                    var gapVertices = new List<Vertex>();
                                    gapVertices.Add(lastSymbolBoundingBox.UpRightPoly);
                                    gapVertices.Add(lastSymbolBoundingBox.DownRightPoly);
                                    gapVertices.Add(boundingBox.UpLeftPoly);
                                    gapVertices.Add(boundingBox.DownLeftPoly);
                                    var gapBoundingBox = new BoundingBox(gapVertices);

                                    if (gapWidth < gapBoundingBox.OuterWidth 
                                        && (lastSymbol.Confidence < 0.9 || symbol.Confidence < 0.9)
                                        && !Helper.IsSeparater(symbol.Text[0]))
                                    {
                                        paragraphEquationList.Add(new Tuple<int, Equation>(index, new Equation()
                                        {
                                            Index = index,
                                            Content = "<gap>",
                                            BoundingBox = gapBoundingBox,
                                        }));
                                        index++;
                                    }
                                }

                                lastSymbolBoundingBox = boundingBox;
                                lastSymbol = symbol;

                                // Check whether has continue chinese to build word, or other character to form equation
                                if (string.IsNullOrEmpty(symbol.Text))
                                {
                                    continue;
                                }
                                else if (Helper.IsChinese(symbol.Text[0]))
                                {
                                    // When separater appear before chinese, include to word
                                    if (tempWord.Length == 0 && paragraphEquationList.Count > 0 && Helper.IsSeparater(paragraphEquationList.Last().Item2.Content[0]))
                                    {
                                        tempWord.Append(paragraphEquationList.Last().Item2.Content[0]);
                                        paragraphWordList.Add(new Tuple<int, ChineseSegment>(paragraphEquationList.Last().Item1, new ChineseSegment()
                                        {
                                            Index = paragraphEquationList.Last().Item1,
                                            Content = paragraphEquationList.Last().Item2.Content,
                                            BoundingBox = paragraphEquationList.Last().Item2.BoundingBox,
                                        }));
                                        paragraphEquationList.RemoveAt(paragraphEquationList.Count - 1);
                                    }

                                    tempWord.Append(symbol.Text);
                                    paragraphWordList.Add(new Tuple<int, ChineseSegment>(index, new ChineseSegment()
                                    {
                                        Index = index,
                                        Content = symbol.Text,
                                        BoundingBox = boundingBox,
                                    }));
                                    index++;
                                }
                                // When separater appear after chinese, include to word
                                else if (Helper.IsSeparater(symbol.Text[0]) && tempWord.Length > 0)
                                {
                                    tempWord.Append(symbol.Text);
                                    paragraphWordList.Add(new Tuple<int, ChineseSegment>(index, new ChineseSegment()
                                    {
                                        Index = index,
                                        Content = symbol.Text,
                                        BoundingBox = boundingBox,
                                    }));
                                    index++;
                                }
                                // Treat single character as a equation, will merge later
                                else
                                {
                                    if (tempWord.Length > 0)
                                    {
                                        tempWord = new StringBuilder();
                                    }

                                    paragraphEquationList.Add(new Tuple<int, Equation>(index, new Equation()
                                    {
                                        Index = index,
                                        Content = symbol.Text,
                                        BoundingBox = boundingBox,
                                    }));
                                    index++;
                                }
                            }
                        }

                        paragraphWordList = Utility.MergeChineseSegments(paragraphWordList);

                        pageWordList.AddRange(paragraphWordList);

                        paragraphEquationList = Utility.MergeEquations(paragraphEquationList);

                        pageEquationList.AddRange(paragraphEquationList);
                    }
                }
            }
        }
    }
}
