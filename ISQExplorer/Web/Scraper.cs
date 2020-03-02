using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using ISQExplorer.Exceptions;
using ISQExplorer.Functional;
using ISQExplorer.Misc;
using ISQExplorer.Models;
using ISQExplorer.Repositories;
using Microsoft.EntityFrameworkCore.Internal;

namespace ISQExplorer.Web
{
    public class Scraper
    {
        private readonly IHtmlClient _htmlClient;
        public ConcurrentSet<DepartmentModel> Departments { get; set; }
        public ITermRepository Terms { get; set; }

        public ConcurrentSet<ISQEntryModel> Entries { get; set; }

        public ConcurrentDictionary<(DepartmentModel Department, string LastName), ProfessorModel> Professors
        {
            get;
            set;
        }

        public ConcurrentBag<ProfessorScrapeException> ProfessorScrapeErrors;

        public ConcurrentDictionary<string, CourseModel> Courses { get; set; }
        public ConcurrentBag<(DepartmentModel, TermModel, CourseScrapeException)> CourseScrapeErrors;

        public HtmlPage? IndexPageCache { get; set; }
        public ConcurrentDictionary<(DepartmentModel, TermModel), HtmlPage> DepartmentListCache { get; set; }
        public ConcurrentDictionary<ProfessorModel, HtmlPage> ProfessorListCache { get; set; }

        private async Task<Try<HtmlPage, IOException>> PageFromUrl(Either<string, Uri> url, string? postData) =>
            await (await (postData == null ? _htmlClient.GetAsync(url) : _htmlClient.PostAsync(url, postData)))
                .SelectAsync(HtmlPage.FromHtmlAsync);


        public Scraper(ITermRepository termRepo, IHtmlClient htmlClient)
        {
            _htmlClient = htmlClient;
            Departments = new ConcurrentSet<DepartmentModel>();
            Terms = termRepo;

            DepartmentListCache = new ConcurrentDictionary<(DepartmentModel, TermModel), HtmlPage>();
            ProfessorListCache = new ConcurrentDictionary<ProfessorModel, HtmlPage>();
        }

        public Task<Result> ScrapeDepartmentsAsync() => Result.OfAsync(async () =>
        {
            if (IndexPageCache == null)
            {
                IndexPageCache = (await PageFromUrl(Urls.DeptSchedule))).Value;
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

            var tab = HtmlTable.Create(tables.Last()).Value;
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

            links.Where(x => !x.HasValue && x.Exception.Element.TextContent.HtmlDecode().IsBlank()).ForEach(val =>
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
                    var (course, title) = val;
                    Courses[course.TextContent] = new CourseModel
                    {
                        CourseCode = course.TextContent,
                        Department = dept,
                        Name = title
                    };
                });
        });

        public Task<Result> ScrapeProfessors(DepartmentModel dept, TermModel term) => Result.OfAsync(async () =>
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

            var tab = HtmlTable.Create(tables.Last()).Value;
            if (!tab.ColumnTitles.Contains("Professor"))
            {
                throw new HtmlElementException(tables.Last(),
                    "Expected a column in the main table titled 'Professor'.");
            }

            var links = tab["Professor"].Select(x => x.Cast<IHtmlAnchorElement>()).ToList();

            links.Where(x => !x.HasValue && x.Exception.Element.TextContent.HtmlDecode().IsBlank()).ForEach(val =>
            {
                ProfessorScrapeErrors.Add(new ProfessorScrapeException(
                    $"The given cell with OuterHTML '{val.Exception.Element.OuterHtml}' was not an <a> element."));
            });

            links.Where(x => x.HasValue).Select(x => x.Value).ForEach(async val =>
            {
                var lname = val.TextContent;
                var nNumber = val.Href.Capture(@"[nN]\d{8}").Select(x => x.ToUpper());
                if (!nNumber)
                {
                    ProfessorScrapeErrors.Add(
                        new ProfessorScrapeException($"The URL {val.Href} does not contain an N-Number."));
                    return;
                }

                if (!Professors.ContainsKey((dept, lname)))
                {
                    var page = (await HtmlPage.FromUrlAsync(Urls.ProfessorPage(nNumber.Value))).Value;

                    var professorName = Try.Of(() => page.QueryAll<IHtmlTableCellElement>("td.dddefault").First(
                        elem => elem.PreviousElementSibling?.TextContent.HtmlDecode().Trim() == "Instructor:"));

                    if (!professorName)
                    {
                        throw new ProfessorScrapeException(
                            $"Could not find instructor name on '{Urls.ProfessorPage(nNumber.Value)}'.",
                            professorName.Exception);
                    }

                    Professors[(dept, lname)] = new ProfessorModel
                    {
                        Department = dept,
                        FirstName = professorName.Value.TextContent.Split(" ").SkipLast(1).Join(" "),
                        LastName = lname,
                        NNumber = nNumber.Value
                    };

                    ProfessorListCache[Professors[(dept, lname)]] = page;
                }
            });
        });

        public Task<Result> ScrapeEntries(ProfessorModel prof) => Result.OfAsync(async () =>
        {
            if (!ProfessorListCache.ContainsKey(prof))
            {
            }
        });
    }
}