using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using ISQExplorer.Exceptions;
using ISQExplorer.Functional;
using ISQExplorer.Misc;

namespace ISQExplorer.Web
{
    public static class ElementExtensions
    {
        public static IEnumerable<T> AnyChildren<T>(this IElement elem) where T : IElement =>
            elem.Children
                .Select(Cast<T>)
                .Where(x => x.HasValue)
                .Select(x => x.Value);

        public static Try<T, HtmlElementException> Cast<T>(this IElement elem) where T : IElement
        {
            if (!(elem is T typedElem))
            {
                return new HtmlElementException(elem, typeof(T));
            }

            return typedElem;
        }

        public static Try<IEnumerable<T>, HtmlElementException> Children<T>(this IElement elem) where T : IElement
        {
            return new Try<IEnumerable<T>, HtmlElementException>(() =>
                elem.Children.Select(e => e.Cast<T>().Value));
        }
    }

    public class HtmlPage
    {
        private readonly IDocument _doc;

        public static async Task<HtmlPage> FromHtmlAsync(string html)
        {
            var config = Configuration.Default;
            var context = BrowsingContext.New(config);
            var doc = await context.OpenAsync(req => req.Content(html));
            return new HtmlPage(doc);
        }

        public static async Task<Try<HtmlPage, IOException>> FromUrlAsync(string url, string? postData = null)
        {
            var res = postData != null ? await Requests.PostAsync(url, postData) : await Requests.GetAsync(url);
            return await res.SelectAsync(FromHtmlAsync);
        }

        public HtmlPage(IDocument doc)
        {
            _doc = doc;
        }

        public Try<T, HtmlElementException> Query<T>(string cssSelector) where T : IElement
        {
            var res = _doc.QuerySelector(cssSelector);
            if (!(res is T typedRes))
            {
                return new HtmlElementException(res, typeof(T));
            }

            return typedRes;
        }

        public IEnumerable<T> QueryAll<T>(string cssSelector) where T : IElement
        {
            var res = _doc.QuerySelectorAll(cssSelector);
            return res.Select(elem => Try.Of(() =>
            {
                if (!(elem is T typedElem))
                {
                    throw new HtmlElementException(elem, typeof(T));
                }

                return typedElem;
            })).Where(x => x.HasValue)
                .Select(x => x.Value);
        }

        public override string ToString()
        {
            return _doc.Prettify();
        }
    }
}