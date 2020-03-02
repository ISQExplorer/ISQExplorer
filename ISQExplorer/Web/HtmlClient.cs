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
        public ConcurrentDictionary<Uri, string> GetCache { get; }
        public ConcurrentDictionary<(Uri, string), string> PostCache { get; }
        public bool UseGetCache { get; }
        public bool UsePostCache { get; }

        public HtmlClient(Func<string, RateLimiter>? limiterFactory = null,
            bool useGetCache = true,
            bool usePostCache = false)
        {
            _domainToLimiter = new ConcurrentDictionary<string, RateLimiter>();
            _limiterFactory = limiterFactory;
            GetCache = new ConcurrentDictionary<Uri, string>();
            PostCache = new ConcurrentDictionary<(Uri, string), string>();
            UseGetCache = useGetCache;
            UsePostCache = usePostCache;
        }

        public HtmlClient(Func<RateLimiter> limiterFactory,
            bool useGetCache = true,
            bool usePostCache = false) : this(s => limiterFactory(), useGetCache, usePostCache)
        {
        }

        public async Task<Try<string, IOException>> GetAsync(Either<Uri, string> url)
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

            if (UseGetCache && GetCache.ContainsKey(u))
            {
                return GetCache[u];
            }

            if (_limiterFactory != null)
            {
                if (!_domainToLimiter.ContainsKey(u.Host))
                {
                    _domainToLimiter[u.Host] = _limiterFactory(u.Host);
                }

                var res = await _domainToLimiter[u.Host].Run(Get);
                if (UseGetCache && res.HasValue)
                {
                    GetCache[u] = res.Value;
                }

                return res;
            }
            else
            {
                var res = await Get();
                if (UseGetCache && res.HasValue)
                {
                    GetCache[u] = res.Value;
                }

                return res;
            }
        }

        public async Task<Try<string, IOException>> PostAsync(Either<Uri, string> url, string postData)
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

            if (UsePostCache && PostCache.ContainsKey((u, postData)))
            {
                return PostCache[(u, postData)];
            }

            if (_limiterFactory != null)
            {
                if (!_domainToLimiter.ContainsKey(u.Host))
                {
                    _domainToLimiter[u.Host] = _limiterFactory(u.Host);
                }

                var res = await _domainToLimiter[u.Host].Run(Post);
                if (UsePostCache && res.HasValue)
                {
                    PostCache[(u, postData)] = res.Value;
                }

                return res;
            }
            else
            {
                var res = await Post();
                if (UsePostCache && res.HasValue)
                {
                    PostCache[(u, postData)] = res.Value;
                }

                return res;
            }
        }

        public Task<Try<string, IOException>> PostAsync(Either<Uri, string> url,
            IDictionary<string, string?> postParams) =>
            PostAsync(url,
                postParams.ToImmutableSortedDictionary()
                    .Select(x => $"{x.Key.HtmlEncode()}={x.Value.HtmlEncode()}")
                    .Join("&"));
    }
}