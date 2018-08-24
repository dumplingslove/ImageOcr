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

        public void QuestionOcr(Image image, out List<Tuple<int, string>> pageWordList, out List<Tuple<int, Equation>> pageEquationList)
        {
            // Call API do the text detection
            TextAnnotation text = this.Client.DetectDocumentText(image);
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine($"Text: {text.Text}");

            pageWordList = new List<Tuple<int, string>>();
            pageEquationList = new List<Tuple<int, Equation>>();
            var index = 0;
            foreach (var page in text.Pages)
            {
                foreach (var block in page.Blocks)
                {
                    string box = string.Join(" - ", block.BoundingBox.Vertices.Select(v => $"({v.X}, {v.Y})"));
                    Console.WriteLine($"Block {block.BlockType} at {box}");
                    foreach (var paragraph in block.Paragraphs)
                    {
                        // Define the vertices index in bounding box object
                        var upLeft = 0;
                        var upRight = 1;
                        var downRight = 2;
                        var downLeft = 3;

                        // Weird behavior, some time the order of vertices can change
                        if (paragraph.BoundingBox.Vertices[0].X == paragraph.BoundingBox.Vertices[1].X)
                        {
                            upLeft = 1;
                            upRight = 2;
                            downRight = 3;
                            downLeft = 0;
                        }

                        box = string.Join(" - ", paragraph.BoundingBox.Vertices.Select(v => $"({v.X}, {v.Y})"));
                        Console.WriteLine($"  Paragraph at {box}");

                        var paragraphWordList = new List<Tuple<int, string>>();
                        var paragraphEquationList = new List<Tuple<int, Equation>>();

                        var tempWord = new StringBuilder();
                        foreach (var word in paragraph.Words)
                        {
                            Console.WriteLine($"    Word: {string.Join("", word.Symbols.Select(s => s.Text))}");

                            foreach (var symbol in word.Symbols)
                            {
                                box = string.Join(" - ", symbol.BoundingBox.Vertices.Select(v => $"({v.X}, {v.Y})"));
                                Console.WriteLine($"      Symbol at {box}");
                                Console.WriteLine($"        Symbol: {symbol.Text}");

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
                                        paragraphEquationList.RemoveAt(paragraphEquationList.Count - 1);
                                    }

                                    tempWord.Append(symbol.Text);
                                }
                                // When separater appear after chinese, include to word
                                else if (Helper.IsSeparater(symbol.Text[0]) && tempWord.Length > 0)
                                {
                                    tempWord.Append(symbol.Text);
                                }
                                // Treat single character as a equation, will merge later
                                else
                                {
                                    if (tempWord.Length > 0)
                                    {
                                        paragraphWordList.Add(new Tuple<int, string>(index, tempWord.ToString()));
                                        index++;
                                        tempWord = new StringBuilder();
                                    }

                                    paragraphEquationList.Add(new Tuple<int, Equation>(index, new Equation()
                                    {
                                        Content = symbol.Text,
                                        Vertices = symbol.BoundingBox.Vertices.ToList(),
                                    }));
                                    index++;
                                }
                            }
                        }

                        if (tempWord.Length > 0)
                        {
                            paragraphWordList.Add(new Tuple<int, string>(index, tempWord.ToString()));
                            index++;
                        }

                        pageWordList.AddRange(paragraphWordList);

                        // Merge the continue single equations
                        if (paragraphEquationList.Count < 2)
                        {
                            pageEquationList.AddRange(paragraphEquationList);
                        }
                        else
                        {
                            var combinedParagraphEquationList = new List<Tuple<int, Equation>>();
                            var vertices = new List<Vertex>();
                            vertices.Add(paragraphEquationList.Last().Item2.Vertices[upLeft]);
                            vertices.Add(paragraphEquationList.Last().Item2.Vertices[upRight]);
                            vertices.Add(paragraphEquationList.Last().Item2.Vertices[downRight]);
                            vertices.Add(paragraphEquationList.Last().Item2.Vertices[downLeft]);

                            var tempEquation = new Tuple<int, Equation>(paragraphEquationList.Last().Item1, new Equation()
                            {
                                Content = paragraphEquationList.Last().Item2.Content,
                                Vertices = vertices,
                            });
                            for (int i = paragraphEquationList.Count - 2; i >= 0; i--)
                            {
                                if (paragraphEquationList[i].Item1 == tempEquation.Item1 - 1
                                    && paragraphEquationList[i].Item2.Vertices[downRight].Y > tempEquation.Item2.Vertices[upLeft].Y)
                                {
                                    var content = paragraphEquationList[i].Item2.Content + tempEquation.Item2.Content;
                                    vertices = new List<Vertex>();
                                    vertices.Add(paragraphEquationList[i].Item2.Vertices[upLeft]);
                                    vertices.Add(tempEquation.Item2.Vertices[1]);
                                    vertices.Add(tempEquation.Item2.Vertices[2]);
                                    vertices.Add(paragraphEquationList[i].Item2.Vertices[downLeft]);

                                    tempEquation = new Tuple<int, Equation>(paragraphEquationList[i].Item1, new Equation()
                                    {
                                        Content = content,
                                        Vertices = vertices,
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

                            pageEquationList.AddRange(combinedParagraphEquationList);
                        }
                    }
                }
            }
        }
    }
}
