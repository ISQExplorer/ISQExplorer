#nullable enable
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ISQExplorer.Functional;
using ISQExplorer.Misc;

namespace ISQExplorer.Web
{
    public static class Requests
    {
        private static HttpClient client = new HttpClient();
        
        public static async Task<Try<string, IOException>> Get(string url)
        {
            try
            {
                client.Timeout = TimeSpan.FromMinutes(30);
                return await client.GetStringAsync(url);
            }
            catch (HttpRequestException e)
            {
                return new IOException($"The server at '{url}' failed to connect.", e);
            }
        }

        public static async Task<Try<string, IOException>> Post(string url, string data)
        {
            try
            {
                client.Timeout = TimeSpan.FromMinutes(30);
                var resp = await client.PostAsync(url, new ByteArrayContent(data.ToBytes()));
                return await resp.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                return new IOException($"The server at '{url}' failed to connect.", e);
            }
        }
    }
}