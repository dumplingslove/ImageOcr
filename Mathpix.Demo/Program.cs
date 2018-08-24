using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathpix.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Mathpix AppId: ");
            var id = Console.ReadLine();
            Console.Write("Mathpix AppKey: ");
            var key = Console.ReadLine();


            var directory = Directory.GetCurrentDirectory();
            while (!Directory.Exists(Path.Combine(directory, "demo")))
            {
                directory = Path.Combine(directory, "..");
                if (!Directory.Exists(directory)) throw new DirectoryNotFoundException("cannot find demo directory");
            }

            foreach (var file in Directory.GetFiles(Path.Combine(directory, "demo"), "*.eq.jpg"))
            {
                if (File.Exists(file + ".output.json")) continue;

                MainAsync(id, key, file).Wait();
            }
        }


        static string template = @"<!DOCTYPE html><html><head><title>MathJax TeX Test Page</title><script type=""text/javascript"" async src=""https://cdn.mathjax.org/mathjax/latest/MathJax.js?config=TeX-AMS_CHTML""></script></head><body>$${0}$$</body></html>";
        static async Task MainAsync(string id, string key, string path)
        {
            var client = new Mathpix.Client.MathpixClient(id, key);

            var result = await client.GetResultJsonAsync(path);
            File.WriteAllText(path + ".output.json", result);

            var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<Mathpix.Client.MathpixResult>(result);
            File.WriteAllText(path + ".output.html", string.Format(template, obj.Latex));
        }
    }
}
