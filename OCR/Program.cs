using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Google.Cloud.Vision.V1;

namespace OCR
{
    class Program
    {
        static void Main(string[] args)
        {
            var directory = Directory.GetCurrentDirectory();
            while (!Directory.Exists(Path.Combine(directory, "Picture")))
            {
                directory = Path.Combine(directory, "..");
                if (!Directory.Exists(directory)) throw new DirectoryNotFoundException("cannot find Picture directory");
            }

            //var imageName = "Math1.png";
            //var imageName = "Geometry1.jpg";
            //var imageName = "Chinese.jpg";
            //var imageName = "PrintWithHandWriting.jpg";
            var imageName = "4.input.jpg.filtered.png";

            var inputPath = Path.Combine(directory, "Picture", imageName);
            var outputFolder = Path.Combine(directory, "Output");
            var outputPath = Path.Combine(outputFolder, "OCR_V1_" + imageName + ".txt");
            var imageFolder = Path.Combine(directory, "Equation");

            Directory.CreateDirectory(outputFolder);
            Directory.CreateDirectory(imageFolder);

            var image = Image.FromFile(inputPath);

            var pageWordList = new List<Tuple<int, string>>();
            var pageEquationList = new List<Tuple<int, Equation>>();

            var googleOcr = new GoogleOcr();
            googleOcr.QuestionOcr(image, out pageWordList, out pageEquationList);

            var rawResults = Utility.CombineRawOcrResult(pageWordList, pageEquationList);
            Utility.OutputResultToFile(outputPath, rawResults);
            Utility.OutputEquationsToImage(imageFolder, inputPath, rawResults);
        }
    }
}
