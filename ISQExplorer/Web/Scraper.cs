using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Html.Dom;
using ISQExplorer.Misc;

namespace ISQExplorer.Web
{
    public class Scraper
    {
        private readonly RateLimiter _limiter;
        public IDictionary<int, string> Departments { get; set; }
        public IDictionary<string, int> Terms { get; set; }
        public IDictionary<int, HtmlPage> CachedPages { get; set; }
        public IDictionary<(int, string), HtmlPage> CachedDepartmentIndexes { get; set; }

        public Scraper(int maxConcurrentTasks = 2, int cycleTimeMillis = 1000)
        {
            _limiter = new RateLimiter(maxConcurrentTasks, cycleTimeMillis);
            Departments = new Dictionary<int, string>();
        }

        private Task<Result> ScrapeDepartmentsAndTerms() => Result.OfAsync(async () =>
        {
            var page = (await _limiter.Run(() => HtmlPage.FromUrlAsync(Urls.DeptSchedule))).Value;

            page.Query<IHtmlSelectElement>("#dept_id").Value
                .Children<IHtmlOptionElement>().Value.Skip(1)
                .ForEach(e => Departments[Parse.Int(e.Id).Value] = e.Label);

            page.Query<IHtmlSelectElement>("#term_id").Value
                .Children<IHtmlOptionElement>().Value.Skip(1)
                .ForEach(e => Terms[e.Label] = Parse.Int(e.Value).Value);
        });
    }
}