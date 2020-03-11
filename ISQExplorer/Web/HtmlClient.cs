using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ISQExplorer.Functional;
using ISQExplorer.Misc;
using ISQExplorer.Repositories;

namespace ISQExplorer.Web
{
    public class HtmlClient : IHtmlClient
    {
        private readonly ConcurrentDictionary<string, RateLimiter> _domainToLimiter;
        private readonly Func<string, RateLimiter>? _limiterFactory;
        public Cache<Uri, HtmlPage> GetCache { get; }
        public Cache<(Uri, string), HtmlPage> PostCache { get; }

        public HtmlClient(Func<string, RateLimiter>? limiterFactory = null)
        {
            _domainToLimiter = new ConcurrentDictionary<string, RateLimiter>();
            _limiterFactory = limiterFactory;
            GetCache = new Cache<Uri, HtmlPage>();
            PostCache = new Cache<(Uri, string), HtmlPage>();
        }

        public HtmlClient(Func<RateLimiter> limiterFactory) : this(s => limiterFactory())
        {
        }

        public async Task<Try<HtmlPage, IOException>> GetAsync(Either<Uri, string> url)
        {
            var u = url.Unite(str => new Uri(str));

            async Task<Try<string, IOException>> Get()
            {
                var request = (HttpWebRequest) WebRequest.Create(u);

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

            return await Try.OfAsync<HtmlPage, IOException>(async () => await GetCache.GetOrMakeAsync(u, async () =>
            {
                if (_limiterFactory != null)
                {
                    if (!_domainToLimiter.ContainsKey(u.Host))
                    {
                        _domainToLimiter[u.Host] = _limiterFactory(u.Host);
                    }

                    var res = await _domainToLimiter[u.Host].Run(Get);
                    return (await res.SelectAsync(HtmlPage.FromHtmlAsync)).ValueOrThrow;
                }
                else
                {
                    var res = await Get();
                    return (await res.SelectAsync(HtmlPage.FromHtmlAsync)).ValueOrThrow;
                }
            }));
        }

        public async Task<Try<HtmlPage, IOException>> PostAsync(Either<Uri, string> url, string postData)
        {
            var u = url.Unite(str => new Uri(str));

            async Task<Try<string, IOException>> Post()
            {
                var request = (HttpWebRequest) WebRequest.Create(u);

                request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                request.UserAgent = "Mozilla/5.0 (X11; Linux x86_64; rv:72.0) Gecko/20100101 Firefox/72.0";

                var payload = postData.ToBytes();

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
                        return new IOException(
                            $"The server at '{url}' returned status code '{response.StatusCode}'");
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

            return await Try.OfAsync<HtmlPage, IOException>(async () => await PostCache.GetOrMakeAsync((u, postData), async () =>
            {
                if (_limiterFactory != null)
                {
                    if (!_domainToLimiter.ContainsKey(u.Host))
                    {
                        _domainToLimiter[u.Host] = _limiterFactory(u.Host);
                    }

                    var res = await _domainToLimiter[u.Host].Run(Post);
                    return (await res.SelectAsync(HtmlPage.FromHtmlAsync)).ValueOrThrow;
                }
                else
                {
                    var res = await Post();
                    return (await res.SelectAsync(HtmlPage.FromHtmlAsync)).ValueOrThrow;
                }
            }));
        }

        public Task<Try<HtmlPage, IOException>> PostAsync(Either<Uri, string> url,
            IReadOnlyDictionary<string, string?> postParams) =>
            PostAsync(url,
                postParams.ToImmutableSortedDictionary()
                    .Select(x => $"{x.Key.HtmlEncode()}={x.Value.HtmlEncode()}")
                    .Join("&"));
    }
}