#nullable enable
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ISQExplorer.Functional;
using ISQExplorer.Misc;

namespace ISQExplorer.Web
{
    public static class Requests
    {
        public static async Task<Try<string, IOException>> Get(string url)
        {
            var request = (HttpWebRequest) WebRequest.Create(url);

            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.UserAgent = "Mozilla/5.0 (X11; Linux x86_64; rv:72.0) Gecko/20100101 Firefox/72.0";

            try
            {
                using var response = (HttpWebResponse) await request.GetResponseAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return new IOException($"The server at '{url}' returned status code '{response.StatusCode}'");
                }

                await using var stream = response.GetResponseStream();
                if (stream == null)
                {
                    return new IOException($"The server at '{url}' did not return a response.");
                }

                using var reader = new StreamReader(stream);
                return await reader.ReadToEndAsync();
            }
            catch (WebException e)
            {
                return new IOException($"The server at '{url}' failed to connect.", e);
            }
        }

        public static async Task<Try<string, IOException>> Post(string url, string data)
        {
            var request = (HttpWebRequest) WebRequest.Create(url);

            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            request.UserAgent = "Mozilla/5.0 (X11; Linux x86_64; rv:72.0) Gecko/20100101 Firefox/72.0";
            
            var payload = data.ToBytes();

            request.Method = "POST";
            request.ContentLength = payload.Length;
            request.Credentials = CredentialCache.DefaultCredentials;
            request.ContentType = "application/x-www-form-urlencoded";

            var dataStream = request.GetRequestStream();
            dataStream.Write(payload, 0, payload.Length);
            dataStream.Close();

            try
            {
                using var response = (HttpWebResponse) await request.GetResponseAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return new IOException($"The server at '{url}' returned status code '{response.StatusCode}'");
                }

                await using var responseStream = response.GetResponseStream();
                if (responseStream == null)
                {
                    return new IOException($"The server at '{url}' did not return a response.");
                }

                using var reader = new StreamReader(responseStream);
                var content = await reader.ReadToEndAsync();

                return content;
            }
            catch (WebException e)
            {
                return new IOException($"The server at '{url}' failed to connect.", e);
            }
        }
    }
}
