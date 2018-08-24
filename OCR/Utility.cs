using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCR
{
    public static class Utility
    {
        public static void OutputResultToFile(string filePath, List<Tuple<int, string>> pageWordList, List<Tuple<int, Equation>> pageEquationList)
        {
            // Output the OCR result
            var dateString = DateTime.Now.ToString("yyyy-mm-dd-ss");
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath))
            {
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
                            file.WriteLine(currentWord.Item1 + " " + currentWord.Item2);
                            wordIndex++;
                        }
                        else
                        {
                            var box = string.Join(" - ", currentEquation.Item2.Vertices.Select(v => $"({v.X}, {v.Y})"));
                            file.WriteLine(currentEquation.Item1 + " " + currentEquation.Item2.Content + " " + box);
                            equationIndex++;
                        }
                    }
                    else if (currentWord != null)
                    {
                        file.WriteLine(currentWord.Item1 + " " + currentWord.Item2);
                        wordIndex++;
                    }
                    else if (currentEquation != null)
                    {
                        var box = string.Join(" - ", currentEquation.Item2.Vertices.Select(v => $"({v.X}, {v.Y})"));
                        file.WriteLine(currentEquation.Item1 + " " + currentEquation.Item2.Content + ": " + box);
                        equationIndex++;
                    }
                }
            }
        }
    }
}
