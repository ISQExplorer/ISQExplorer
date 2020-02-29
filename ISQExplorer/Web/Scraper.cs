using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using ISQExplorer.Exceptions;
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

        public ConcurrentBag<ISQEntryModel> Entries { get; set; }

        public ConcurrentDictionary<(DepartmentModel Department, string LastName), ProfessorModel> Professors
        {
            get;
            set;
        }

        public ConcurrentDictionary<string, CourseModel> Courses { get; set; }
        public ConcurrentBag<(DepartmentModel, TermModel, CourseScrapeException)> CourseScrapeErrors;

        public HtmlPage? IndexPageCache { get; set; }
        public IDictionary<(DepartmentModel, TermModel), HtmlPage> DepartmentListCache { get; set; }
        public IDictionary<ProfessorModel, HtmlPage> ProfessorListCache { get; set; }

        public Scraper(ITermRepository termRepo)
        {
            _limiter = new RateLimiter(2, 1000);
            Departments = new HashSet<DepartmentModel>();
            Terms = termRepo;

            DepartmentListCache = new Dictionary<(DepartmentModel, TermModel), HtmlPage>();
            ProfessorListCache = new Dictionary<ProfessorModel, HtmlPage>();
        }

        public Task<Result> ScrapeDepartmentsAsync() => Result.OfAsync(async () =>
        {
            if (IndexPageCache == null)
            {
                IndexPageCache = (await _limiter.Run(() => HtmlPage.FromUrlAsync(Urls.DeptSchedule))).Value;
            }

            IndexPageCache.Query<IHtmlSelectElement>("#dept_id").Value
                .Children<IHtmlOptionElement>().Value.Skip(1)
                .ForEach(e => Departments.Add(new DepartmentModel {Id = Parse.Int(e.Id).Value, Name = e.Label}));
        });

        public Task<Result> ScrapeTermsAsync() => Result.OfAsync(async () =>
        {
            if (IndexPageCache == null)
            {
                IndexPageCache = (await _limiter.Run(() => HtmlPage.FromUrlAsync(Urls.DeptSchedule))).Value;
            }

            IndexPageCache.Query<IHtmlSelectElement>("#term_id").Value
                .Children<IHtmlOptionElement>().Value.Skip(1)
                .ForEach(e => Terms.Add(new TermModel {Name = e.Label, Id = Parse.Int(e.Value).Value}));
        });

        public Task<Result> ScrapeCourses(DepartmentModel dept, TermModel term) => Result.OfAsync(async () =>
        {
            if (!DepartmentListCache.ContainsKey((dept, term)))
            {
                DepartmentListCache[(dept, term)] = (await _limiter.Run(() =>
                    HtmlPage.FromUrlAsync(Urls.DeptSchedule, Urls.DeptSchedulePostData(term.Id, dept.Id)))).Value;
            }

            var page = DepartmentListCache[(dept, term)];
            var tables = page.QueryAll<IHtmlTableElement>("table.datadisplaytable").ToList();
            if (tables.Count != 3)
            {
                throw new HtmlPageException(page,
                    "This page does not have the required number (3) of table.datadisplaytable.");
            }

            var tab = new HtmlTable(tables.Last());
            if (!tab.ColumnTitles.Contains("Course"))
            {
                throw new HtmlElementException(tables.Last(), "Expected a column in the main table titled 'Course'.");
            }

            var links = tab["Course"].Select(x => x.Cast<IHtmlAnchorElement>()).ToList();
            var titles = tab["Title"].ToList();

            if (links.Count != titles.Count)
            {
                throw new WtfException("Different lengths of Course and Title column in the same table.");
            }

            links.Where(x => !x.HasValue).ForEach(val =>
            {
                CourseScrapeErrors.Add((dept, term,
                    new CourseScrapeException(val.Exception.Element.TextContent,
                        "The given cell was not an <a> element.")));
            });

            links
                .Zip(titles)
                .Where(x => x.First.HasValue)
                .Select(x => (Course: x.First.Value, Title: x.Second.TextContent))
                .ForEach(val =>
                {
                    Courses[val.Course.TextContent] = new CourseModel
                    {
                        CourseCode = val.Course.TextContent,
                        Department = dept,
                        Name = val.Title
                    };
                });
        });
    }
}