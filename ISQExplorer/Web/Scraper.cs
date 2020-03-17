using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Html.Dom;
using ISQExplorer.Controllers;
using ISQExplorer.Exceptions;
using ISQExplorer.Functional;
using ISQExplorer.Misc;
using ISQExplorer.Models;
using ISQExplorer.Repositories;

namespace ISQExplorer.Web
{
    public class Scraper
    {
        public IHtmlClient HtmlClient { get; set; }
        public IDepartmentRepository Departments { get; set; }
        public ITermRepository Terms { get; set; }

        public IEntryRepository Entries { get; set; }
        public IProfessorRepository Professors { get; set; }
        public ICourseRepository Courses { get; set; }

        public ConcurrentBag<Exception> Errors { get; set; }

        public Scraper(ITermRepository termRepo, IProfessorRepository profRepo, IDepartmentRepository departmentRepo,
            IEntryRepository entryRepo, ICourseRepository courseRepo, IHtmlClient htmlClient)
        {
            Terms = termRepo;
            Professors = profRepo;
            HtmlClient = htmlClient;
            Departments = departmentRepo;
            Courses = courseRepo;
            Entries = entryRepo;
            Errors = new ConcurrentBag<Exception>();
        }

        public Task<Result> ScrapeDepartmentsAsync() => Result.OfAsync(async () =>
        {
            await (await HtmlClient.GetAsync(Urls.DeptSchedule)).Value.Query<IHtmlSelectElement>("#dept_id").Value
                .Children<IHtmlOptionElement>().Value.Skip(1)
                .ForEachAsync(async e =>
                    await Departments.AddAsync(new DepartmentModel {Id = Parse.Int(e.Value).Value, Name = e.Label}));
        });

        public Task<Result> ScrapeTermsAsync() => Result.OfAsync(async () =>
        {
            await Terms.AddRangeAsync((await HtmlClient.GetAsync(Urls.DeptSchedule)).Value
                .Query<IHtmlSelectElement>("#term_id")
                .Value
                .Children<IHtmlOptionElement>().Value.Skip(1)
                .Select(e => new TermModel {Name = e.Label, Id = Parse.Int(e.Value).Value}));
        });

        public Task<Result> ScrapeCoursesAsync(DepartmentModel dept, TermModel term) => Result.OfAsync(async () =>
        {
            var page = (await HtmlClient.PostAsync(Urls.DeptSchedule, Urls.DeptSchedulePostData(term.Id, dept.Id)))
                .Value;
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

            var links = tab["Course"]
                .Where(x => x.Children.Length == 1)
                .Select(x => x.Children.First().Cast<IHtmlAnchorElement>())
                .ToList();

            var titles = tab["Title"].ToList();

            if (links.Count != titles.Count)
            {
                throw new WtfException($"Different lengths of Course and Title column in the same table for dept schedule {Urls.DeptSchedulePostData(term.Id, dept.Id)}.");
            }

            links.Where(x => !x.HasValue && !x.Exception.Element.TextContent.HtmlDecode().IsBlank()).ForEach(val =>
            {
                Errors.Add(new CourseScrapeException(
                    "The given cell was not an <a> element.", val.Exception.Element.TextContent, dept, term));
            });

            await links
                .Zip(titles)
                .Where(x => x.First.HasValue)
                .Select(x => (Course: x.First.Value, Title: x.Second.TextContent))
                .ForEachAsync(async val =>
                {
                    var (course, title) = val;
                    await Courses.AddAsync(new CourseModel
                    {
                        CourseCode = course.TextContent,
                        Department = dept,
                        Name = title
                    });
                });
        });

        public Task<Result> ScrapeProfessorsAsync(DepartmentModel dept, TermModel term) => Result.OfAsync(async () =>
        {
            var page = (await HtmlClient.PostAsync(Urls.DeptSchedule, Urls.DeptSchedulePostData(term.Id, dept.Id)))
                .Value;
            var tables = page.QueryAll<IHtmlTableElement>("table.datadisplaytable").ToList();
            if (tables.Count != 3)
            {
                throw new HtmlPageException(page,
                    "This page does not have the required number (3) of table.datadisplaytable.");
            }

            var tab = HtmlTable.Create(tables.Last()).Value;
            if (!tab.ColumnTitles.Contains("Instructor"))
            {
                throw new HtmlElementException(tables.Last(),
                    "Expected a column in the main table titled 'Instructor'.");
            }

            var links = tab["Instructor"]
                .Where(x => x.Children.Length == 1)
                .Select(x => x.Children.First().Cast<IHtmlAnchorElement>()).ToList();

            links.Where(x => !x.HasValue && !x.Exception.Element.TextContent.HtmlDecode().IsBlank()).ForEach(val =>
            {
                Errors.Add(new ProfessorScrapeException(
                    $"The given cell with OuterHTML '{val.Exception.Element.OuterHtml}' was not an <a> element.", null,
                    dept, term));
            });

            var nNumbers = links.Values()
                .Where(x => !x.TextContent.HtmlDecode().IsBlank())
                .Select(x => Try.Of(() =>
                {
                    var nNumber = x.Href.Capture(@"[nN]\d{8}").Select(y => y.ToUpper());
                    if (!nNumber)
                    {
                        throw new ProfessorScrapeException($"The URL '{x.Href}' does not contain an N-Number.",
                            null,
                            dept,
                            term);
                    }

                    return nNumber.Value;
                }))
                .ToList();

            nNumbers.Exceptions().ForEach(ex => Errors.Add(ex));

            await nNumbers.Values().ToHashSet().AsParallel().ForEachAsync(async nNumber =>
            {
                if (await Professors.FromNNumberAsync(dept, nNumber)) return;

                var htmlPage = (await HtmlClient.GetAsync(Urls.ProfessorPage(nNumber))).Value;

                var professorName = Try.Of(() => htmlPage.QueryAll<IHtmlTableCellElement>("td.dddefault").First(
                    elem => elem.PreviousElementSibling?.TextContent.HtmlDecode().Trim() == "Instructor:"));

                if (!professorName)
                {
                    Errors.Add(new ProfessorScrapeException(
                        $"Could not find instructor name on '{Urls.ProfessorPage(nNumber)}'.",
                        professorName.Exception, nNumber, dept, term));
                    return;
                }

                await Professors.AddAsync(new ProfessorModel
                {
                    Department = dept,
                    FirstName = professorName.Value.TextContent.Split(" ").SkipLast(1).Join(" "),
                    LastName = professorName.Value.TextContent.Split(" ").Last(),
                    NNumber = nNumber
                });
            });
        });

        public Task<Result> ScrapeProfessorEntriesAsync(ProfessorModel prof) => Result.OfAsync(async () =>
        {
            var page = await HtmlClient.GetAsync(Urls.ProfessorPage(prof.NNumber));
            if (!page)
            {
                Errors.Add(page.Exception);
                return;
            }

            var tables = page.Value.QueryAll<IHtmlTableElement>("table.datadisplaytable").ToList();
            if (tables.Count != 6)
            {
                Errors.Add(new HtmlPageException(page.Value, $"Expected 6 tables, got {tables.Count}"));
                return;
            }

            var mainTable = HtmlTable.Create(tables[3]);
            if (!mainTable)
            {
                Errors.Add(mainTable.Exception);
                return;
            }

            var gpaTable = HtmlTable.Create(tables[5]);
            if (!gpaTable)
            {
                Errors.Add(mainTable.Exception);
                return;
            }

            var mainRows = mainTable.Value.Rows
                .GroupBy(x => (Term: x["Term"], Crn: x["CRN"], CourseCode: x["Course ID"]))
                .ToDictionary(x => x.Key, x => x.First());

            var gpaRows = gpaTable.Value.Rows.GroupBy(x => (Term: x["Term"], Crn: x["CRN"], CourseCode: x["Course ID"]))
                .ToDictionary(x => x.Key, x => x.First());

            /*
            var mrKeys = mainRows
                .Select(x => new[] {x.Key.Term.TextContent, x.Key.Crn.TextContent, x.Key.CourseCode.TextContent}.Join(", "))
                .OrderBy(x => x).ToList();
            var gpaKeys = gpaRows.Select(x => new[] {x.Key.Term.TextContent, x.Key.Crn.TextContent, x.Key.CourseCode.TextContent}.Join(", "))
                .OrderBy(x => x).ToList();

            var pairs = (from mrow in mainRows
                join grow in gpaRows on
                    (mrow.Key.Crn.TextContent, mrow.Key.Term.TextContent, mrow.Key.CourseCode.TextContent) equals
                    (grow.Key.Crn.TextContent, grow.Key.Term.TextContent, grow.Key.CourseCode.TextContent)
                select (mrow.Value, grow.Value)).ToList();
                */

            var results = await Task.WhenAll((from mrow in mainRows
                join grow in gpaRows on
                    (mrow.Key.Crn.TextContent.HtmlDecode().Trim(), mrow.Key.Term.TextContent.HtmlDecode().Trim(),
                        mrow.Key.CourseCode.TextContent.HtmlDecode().Trim()) equals
                    (grow.Key.Crn.TextContent.HtmlDecode().Trim(), grow.Key.Term.TextContent.HtmlDecode().Trim(),
                        grow.Key.CourseCode.TextContent.HtmlDecode().Trim())
                select (mrow.Value, grow.Value)).Select(async group => await Result.OfAsync(async () =>
            {
                var (mrow, grow) = group;

                await Entries.AddAsync(new ISQEntryModel
                {
                    Course = (await Courses.FromCourseCodeAsync(mrow["Course ID"].TextContent)).Value,
                    Term = (await Terms.FromStringAsync(mrow["Term"].TextContent)).Value,
                    Professor = prof,
                    Crn = Parse.Int(mrow["CRN"].TextContent).Value,
                    NEnrolled = Parse.Int(mrow["Number Enrolled"].TextContent).Value,
                    NResponded = Parse.Int(mrow["Number Responded"].TextContent).Value,
                    Pct5 = Parse.Double(mrow["Excellent (5)"].TextContent).Value,
                    Pct4 = Parse.Double(mrow["Very Good (4)"].TextContent).Value,
                    Pct3 = Parse.Double(mrow["Good (3)"].TextContent).Value,
                    Pct2 = Parse.Double(mrow["Fair (2)"].TextContent).Value,
                    Pct1 = Parse.Double(mrow["Poor (1)"].TextContent).Value,
                    PctNa = Parse.Double(mrow["NR/NA"].TextContent).Value,
                    PctA = Parse.Double(grow["A"].TextContent).Value,
                    PctAMinus = Parse.Double(grow["A-"].TextContent).Value,
                    PctBPlus = Parse.Double(grow["B+"].TextContent).Value,
                    PctB = Parse.Double(grow["B"].TextContent).Value,
                    PctBMinus = Parse.Double(grow["B-"].TextContent).Value,
                    PctCPlus = Parse.Double(grow["C+"].TextContent).Value,
                    PctC = Parse.Double(grow["C"].TextContent).Value,
                    PctD = Parse.Double(grow["D"].TextContent).Value,
                    PctF = Parse.Double(grow["F"].TextContent).Value,
                    PctWithdraw = Parse.Double(grow["Withdraw"].TextContent).Value,
                    MeanGpa = Parse.Double(grow["Mean GPA"].TextContent).Value
                });
            })));

            results.Where(res => res.IsError).Select(res => res.Error).ForEach(Errors.Add);
        });

        public Task<Result> ScrapeEntriesAsync() => Result.OfAsync(async () =>
        {
            if (Departments.None())
            {
                var res = await ScrapeDepartmentsAsync();
                if (!res)
                {
                    return res;
                }
            }

            if (Terms.None())
            {
                var res = await ScrapeTermsAsync();
                if (!res)
                {
                    return res;
                }
            }

            await Departments.SelectMany(x => Terms.Terms,
                (model, termModel) => (model, termModel)).AsParallel().ForEachAsync(async x =>
            {
                var (dept, term) = x;

                var res = await ScrapeCoursesAsync(dept, term) && await ScrapeProfessorsAsync(dept, term);
                if (!res)
                {
                    Errors.Add(res.Error);
                }
            });

            await Professors.AsParallel().ForEachAsync(async prof => await ScrapeProfessorEntriesAsync(prof));
            return new Result();
        });

        public Task SaveChangesAsync() => Task.WhenAll(
            Courses.SaveChangesAsync(),
            Departments.SaveChangesAsync(),
            Entries.SaveChangesAsync(),
            Professors.SaveChangesAsync(),
            Terms.SaveChangesAsync()
        );
    }
}