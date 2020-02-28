using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Html.Dom;
using ISQExplorer.Misc;
using ISQExplorer.Models;
using ISQExplorer.Repositories;

namespace ISQExplorer.Web
{
    public class Scraper
    {
        private readonly RateLimiter _limiter;
        public ICollection<DepartmentModel> Departments { get; set; }
        public ITermRepository Terms { get; set; }

        public HtmlPage? IndexPageCache { get; set; }
        public IDictionary<(int, TermModel), HtmlPage> DepartmentListCache { get; set; }
        public IDictionary<ProfessorModel, HtmlPage> ProfessorListCache { get; set; }


        public Scraper(ITermRepository termRepo, int maxConcurrentTasks = 2, int cycleTimeMillis = 1000)
        {
            _limiter = new RateLimiter(maxConcurrentTasks, cycleTimeMillis);
            Departments = new HashSet<DepartmentModel>();
            
            DepartmentListCache = new Dictionary<(int, TermModel), HtmlPage>();
            ProfessorListCache = new Dictionary<ProfessorModel, HtmlPage>();
        }

        private Task<Result> ScrapeDepartmentsAsync() => Result.OfAsync(async () =>
        {
            if (IndexPageCache == null)
            {
                IndexPageCache = (await _limiter.Run(() => HtmlPage.FromUrlAsync(Urls.DeptSchedule))).Value;
            }

            IndexPageCache.Query<IHtmlSelectElement>("#dept_id").Value
                .Children<IHtmlOptionElement>().Value.Skip(1)
                .ForEach(e => Departments.Add(new DepartmentModel {Id = Parse.Int(e.Id).Value, Name = e.Label}));
        });

        private Task<Result> ScrapeTermsAsync() => Result.OfAsync(async () =>
        {
            if (IndexPageCache == null)
            {
                IndexPageCache = (await _limiter.Run(() => HtmlPage.FromUrlAsync(Urls.DeptSchedule))).Value;
            }

            IndexPageCache.Query<IHtmlSelectElement>("#term_id").Value
                .Children<IHtmlOptionElement>().Value.Skip(1)
                .ForEach(e => Terms.Add(new TermModel {Name = e.Label, Id = Parse.Int(e.Value).Value}));
        });
    }
}