using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using ISQExplorer.Exceptions;
using ISQExplorer.Functional;
using ISQExplorer.Misc;
using ISQExplorer.Models;
using ISQExplorer.Repositories;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore.Internal;

namespace ISQExplorer.Web
{
    public class Scraper
    {
        private readonly IHtmlClient _htmlClient;
        public ConcurrentSet<DepartmentModel> Departments { get; set; }
        public ITermRepository Terms { get; set; }

        public ConcurrentSet<ISQEntryModel> Entries { get; set; }

        public ConcurrentDictionary<DepartmentModel, ConcurrentDictionary<string, ProfessorModel>> Professors
        {
            get;
            set;
        }

        public ConcurrentDictionary<string, CourseModel> Courses { get; set; }

        public ConcurrentBag<Exception> Errors { get; set; }

        private readonly ConcurrentDictionary<(Either<string, Uri>, string?), HtmlPage> _pageCache;

        private async Task<Try<HtmlPage, IOException>> PageFromUrl(Either<string, Uri> url, string? postData = null)
        {
            if (_pageCache.ContainsKey((url, postData)))
            {
                return _pageCache[(url, postData)];
            }

            var res =
                await (await (postData == null ? _htmlClient.GetAsync(url) : _htmlClient.PostAsync(url, postData)))
                    .SelectAsync(HtmlPage.FromHtmlAsync);

            if (res.HasValue)
            {
                _pageCache[(url, postData)] = res.Value;
            }

            return res;
        }


        public Scraper(ITermRepository termRepo, IHtmlClient htmlClient)
        {
            _htmlClient = htmlClient;
            Departments = new ConcurrentSet<DepartmentModel>();
            Terms = termRepo;
            Courses = new ConcurrentDictionary<string, CourseModel>();
            _pageCache = new ConcurrentDictionary<(Either<string, Uri>, string), HtmlPage>();
            Errors = new ConcurrentBag<Exception>();
            Professors = new ConcurrentDictionary<DepartmentModel, ConcurrentDictionary<string, ProfessorModel>>();
        }

        public Task<Result> ScrapeDepartmentsAsync() => Result.OfAsync(async () =>
        {
            (await PageFromUrl(Urls.DeptSchedule)).Value.Query<IHtmlSelectElement>("#dept_id").Value
                .Children<IHtmlOptionElement>().Value.Skip(1)
                .ForEach(e => Departments.Add(new DepartmentModel {Id = Parse.Int(e.Id).Value, Name = e.Label}));
        });

        public Task<Result> ScrapeTermsAsync() => Result.OfAsync(async () =>
        {
            (await PageFromUrl(Urls.DeptSchedule)).Value.Query<IHtmlSelectElement>("#term_id").Value
                .Children<IHtmlOptionElement>().Value.Skip(1)
                .ForEach(e => Terms.Add(new TermModel {Name = e.Label, Id = Parse.Int(e.Value).Value}));
        });

        public Task<Result> ScrapeCourses(DepartmentModel dept, TermModel term) => Result.OfAsync(async () =>
        {
            var page = (await PageFromUrl(Urls.DeptSchedule, Urls.DeptSchedulePostData(term.Id, dept.Id))).Value;
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
                Errors.Add(new CourseScrapeException(
                    "The given cell was not an <a> element.", val.Exception.Element.TextContent, dept, term));
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
            var page = (await PageFromUrl(Urls.DeptSchedule, Urls.DeptSchedulePostData(term.Id, dept.Id))).Value;
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
                Errors.Add(new ProfessorScrapeException(
                    $"The given cell with OuterHTML '{val.Exception.Element.OuterHtml}' was not an <a> element.", null,
                    dept, term));
            });

            links.Where(x => x.HasValue).Select(x => x.Value).ForEach(async val =>
            {
                var lname = val.TextContent;
                var nNumber = val.Href.Capture(@"[nN]\d{8}").Select(x => x.ToUpper());
                if (!nNumber)
                {
                    Errors.Add(
                        new ProfessorScrapeException($"The URL {val.Href} does not contain an N-Number.", null, dept,
                            term));
                    return;
                }

                if (!Professors.ContainsKey((dept, lname)))
                {
                    var page = (await PageFromUrl(Urls.ProfessorPage(nNumber.Value))).Value;

                    var professorName = Try.Of(() => page.QueryAll<IHtmlTableCellElement>("td.dddefault").First(
                        elem => elem.PreviousElementSibling?.TextContent.HtmlDecode().Trim() == "Instructor:"));

                    if (!professorName)
                    {
                        throw new ProfessorScrapeException(
                            $"Could not find instructor name on '{Urls.ProfessorPage(nNumber.Value)}'.",
                            professorName.Exception, nNumber.Value, dept, term);
                    }

                    Professors[(dept, lname)] = new ProfessorModel
                    {
                        Department = dept,
                        FirstName = professorName.Value.TextContent.Split(" ").SkipLast(1).Join(" "),
                        LastName = lname,
                        NNumber = nNumber.Value
                    };
                }
            });
        });

        public Task<Result> ScrapeEntries(ProfessorModel prof, bool recursive = true) => Result.OfAsync(async () =>
        {
            if (recursive && Departments.None())
            {
                var res = await ScrapeDepartmentsAsync();
                if (!res)
                {
                    return res;
                }
            }

            Departments.SelectMany(x => Terms.Terms,
                (model, termModel) => (model, termModel)).AsParallel().ForEach(async x =>
            {
                var (dept, term) = x;

                if (recursive && Courses.None())
                {
                    var res = await ScrapeCourses(dept, term);
                    if (!res)
                    {
                        Errors.Add(res.Error);
                        return;
                    }
                }

                if (recursive && Professors.None())
                {
                    var res = await ScrapeProfessors(dept, term);
                    if (!res)
                    {
                        Errors.Add(res.Error);
                        return;
                    }
                }

                var page = await PageFromUrl(Urls.DeptSchedule, Urls.DeptSchedulePostData(term.Id, dept.Id));
                if (!page)
                {
                    Errors.Add(page.Exception);
                    return;
                }

                var tables = page.Value.QueryAll<IHtmlTableElement>("table.datadisplaytable").ToList();
                if (tables.Count != 3)
                {
                    Errors.Add(new HtmlPageException(page.Value,
                        "This page does not have the required number (3) of table.datadisplaytable."));
                    return;
                }

                var tab = HtmlTable.Create(tables.Last()).Value;
                if (!tab.ColumnTitles.Contains("Professor"))
                {
                    Errors.Add(new HtmlElementException(tables.Last(),
                        "Expected a column in the main table titled 'Professor'."));
                }
            });

            return new Result();
        });
    }
}