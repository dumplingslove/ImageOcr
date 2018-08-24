using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Mathpix.Client
{
    public class MathpixClient
    {
        private HttpClient client;

        public MathpixClient(string appId, string appKey)
        {
            client = new HttpClient
            {
                BaseAddress = new Uri("https://api.mathpix.com/v3/latex")
            };

            client.DefaultRequestHeaders.Add("app_id", appId);
            client.DefaultRequestHeaders.Add("app_key", appKey);
            
        }
        
        public async Task<string> GetResultJsonAsync(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                return await GetResultJsonAsync(stream);
            }
        }

        public async Task<string> GetResultJsonAsync(Stream stream)
        {
            byte[] data = null;
            using (MemoryStream ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                data = ms.ToArray();
            }
            
            var content = "{ \"src\" : \"data:image/jpeg;base64," + Convert.ToBase64String(data) + "\" }";

            var response = await client.PostAsync("", new StringContent(content, Encoding.UTF8, "application/json"));
            response = response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
    }
}
