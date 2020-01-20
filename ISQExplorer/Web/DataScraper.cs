#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using ISQExplorer.Exceptions;
using ISQExplorer.Functional;
using ISQExplorer.Misc;
using ISQExplorer.Models;

namespace ISQExplorer.Web
{
    public static class DataScraper
    {
        private static readonly RateLimiter Limiter = new RateLimiter(2, 1000);

        public static async Task<IDocument> ToDocument(string html)
        {
            var config = Configuration.Default;
            var context = BrowsingContext.New(config);
            var doc = await context.OpenAsync(req => req.Content(html));
            return doc;
        }

        public static async Task<Try<IDocument, IOException>> ToDocument(Try<string, IOException> html)
        {
            return html ? new Try<IDocument, IOException>(await ToDocument(html.Value)) : html.Exception;
        }

        public static IEnumerable<IHtmlTableCellElement> RowChildren(this IHtmlTableRowElement row)
        {
            foreach (var child in row.Children)
            {
                if (!(child is IHtmlTableCellElement cell))
                {
                    throw new ArgumentException($"Non cell child '{child.InnerHtml}' of row '{row.InnerHtml}'");
                }

                for (var i = 0; i < cell.ColumnSpan; ++i)
                {
                    yield return cell;
                }
            }
        }

        /// <summary>
        /// Retrieves a list of departments from the web.
        /// </summary>
        /// <returns>A Try containing a list of departments, or an IOException detailing why it could not be returned.</returns>
        /// <exception cref="IOException">Details the type of error.</exception>
        public static async Task<Try<IEnumerable<DepartmentModel>>> ScrapeDepartmentIds()
        {
            const string url = "https://bannerssb.unf.edu/nfpo-ssb/wksfwbs.p_dept_schd";
            var html = await Limiter.Run(() => Requests.Get(url));
            if (!html)
            {
                return new IOException("Error while retrieving departments.", html.Exception);
            }

            var document = await ToDocument(html.Value);
            var selectors = document.QuerySelectorAll("#dept_id").ToList();
            if (selectors.Count != 1)
            {
                return new IOException(
                    $"Encountered malformed page while retrieving departments. Expected one #dept_id, found {selectors.Count}.");
            }

            return new Try<IEnumerable<DepartmentModel>, IOException>(() => selectors.First().Children.Skip(1).Select(
                x =>
                {
                    if (!(x is IHtmlOptionElement opt))
                    {
                        throw new MalformedPageException(
                            $"Non option element found as child of selector with OuterHTML '{x.OuterHtml}'.");
                    }

                    var id = Parse.Int(opt.Value);
                    var label = opt.Label;

                    if (!id)
                    {
                        throw new MalformedPageException($"Invalid id found '{opt.Value}'.");
                    }

                    return new DepartmentModel {Id = id.Value, Name = label};
                }));
        }

        /// <summary>
        /// Scrapes courses and professors from a department.
        /// Use <see cref="ScrapeDepartmentIds"/> to get a list of departments.
        /// </summary>
        /// <param name="dept">The department to scrape.</param>
        /// <param name="when">The Term to scrape entries from. By default this is one semester before the current one
        /// So if it is currently Fall 2019, this function would scrape entries from Summer 2019.</param>
        /// <returns>An Optional containing an IOException on failure, or nothing on success.</returns>
        public static async Task<Try<
            (
            IEnumerable<CourseModel> Courses,
            IEnumerable<ProfessorModel> Professors,
            IEnumerable<MalformedPageException> Exceptions
            ),
            IOException
        >> ScrapeDepartment(
            DepartmentModel dept,
            Term? when = null
        )
        {
            var courses = new ConcurrentSet<CourseModel>();
            var professors = new ConcurrentSet<ProfessorModel>();
            var exceptions = new ConcurrentBag<MalformedPageException>();

            var (season, year) = when ?? new Term(DateTime.UtcNow) - 1;
            var no = year * 100;
            no += season switch
            {
                Season.Spring => 10,
                Season.Summer => 50,
                Season.Fall => 80,
                _ => 0
            };

            var document = await ToDocument(await Limiter.Run(() =>
                Requests.Post("https://bannerssb.unf.edu/nfpo-ssb/wksfwbs.p_dept_schd",
                    $"pv_term={no}&pv_dept={dept.Id}&pv_ptrm=&pv_campus=&pv_sub=Submit")));
            if (!document.HasValue)
            {
                return new IOException($"Error while retrieving department page {dept}.", document.Exception);
            }

            var urlToProf = new ConcurrentDictionary<string, ProfessorModel>();

            var res = await document.Value
                .QuerySelectorAll("table.datadisplaytable > tbody > tr")
                .Skip(3)
                .TryAllParallel(async x =>
                {
                    if (!(x is IHtmlTableRowElement row))
                    {
                        return new ArgumentException($"Expected row, got element '{x.OuterHtml}'");
                    }

                    var children = row.RowChildren().ToList();

                    if (children.Count < 18)
                    {
                        exceptions.Add(new MalformedPageException(
                            $"Malformed row. Incorrect number of columns in table. Expected at least 18, got {children.Count}\nRow: '{x.InnerHtml}'"));
                        return null;
                    }

                    if (children[2].Children.Length == 0 || children[2].Children.First().InnerHtml.Trim() == "")
                    {
                        exceptions.Add(new MalformedPageException(
                            $"Blank course code in cell '{children[2].InnerHtml}' with row '{row.InnerHtml}'"
                        ));
                        return null;
                    }

                    courses.Add(new CourseModel
                    {
                        CourseCode = children[2].Children.First().InnerHtml,
                        Department = dept,
                        Name = children[3].InnerHtml
                    });

                    if (WebUtility.HtmlDecode(children.First().InnerHtml)?.Trim() != "")
                    {
                        return null;
                    }

                    if (children[17].Children.None() ||
                        !(children[17].Children.First() is IHtmlAnchorElement professorCell))
                    {
                        exceptions.Add(
                            new MalformedPageException($"Expected anchor element in 17th column, did not get one."));
                        return null;
                    }

                    var url = $"https://banner.unf.edu/pls/nfpo{professorCell.PathName}{professorCell.Search}";

                    if (!urlToProf.ContainsKey(url))
                    {
                        var profTry = await ScrapeDepartmentProfessor(url, dept);

                        if (!profTry)
                        {
                            exceptions.Add(new MalformedPageException("Failed to scrape professor data",
                                profTry.Exception));
                            return null;
                        }

                        urlToProf[url] = profTry.Value;
                    }

                    professors.Add(urlToProf[url]);
                    return null;
                });

            return res
                ? new Try<(IEnumerable<CourseModel> Courses, IEnumerable<ProfessorModel> Professors,
                    IEnumerable<MalformedPageException> Exceptions), IOException>(
                    new IOException("Error scraping the department.", res.Value))
                : (courses, professors, exceptions);
        }


        public static async Task<Try<IEnumerable<ISQEntryModel>, IOException>> ScrapeDepartmentCourseEntries(
            CourseModel course,
            Func<string, Try<ProfessorModel>> lastNameToProfessor
        ) => await ScrapeDepartmentCourseEntries(course, lastNameToProfessor.ToAsync());


        public static async Task<Try<IEnumerable<ISQEntryModel>, IOException>> ScrapeDepartmentCourseEntries(
            CourseModel course,
            Func<string, Task<Try<ProfessorModel>>> lastNameToProfessor
        )
        {
            var url =
                $"https://bannerssb.unf.edu/nfpo-ssb/wksfwbs.p_course_isq_grade?pv_course_id={course.CourseCode}";
            var document = await ToDocument(await Limiter.Run(() => Requests.Get(url)));
            if (!document.HasValue)
            {
                return new IOException($"Error while scraping course code '{course.CourseCode}'.", document.Exception);
            }

            var tables = document.Value.QuerySelectorAll("table.datadisplaytable > tbody").ToList();
            if (tables.Count < 6)
            {
                return new IOException(
                    $"Malformed page at url '{url}'. Most likely the given course code '{course.CourseCode}' is invalid. Returning a blank list.");
            }

            var isqTable = tables[3];
            var gpaTable = tables[5];

            return (await Task.WhenAll(isqTable.Children.Skip(2).Zip(gpaTable.Children.Skip(2)).Select(async x =>
            {
                var (isq, gpa) = x;
                var childText = isq.Children.Select(y =>
                    y.Children.Length == 0 ? y.InnerHtml.Trim() : y.Children.First().InnerHtml.Trim()).ToList();
                var gpaText = gpa.Children.Select(y =>
                    y.Children.Length == 0 ? y.InnerHtml.Trim() : y.Children.First().InnerHtml.Trim()).ToList();
                var (season, year) = new Term(childText[0]);
                var professor = lastNameToProfessor != null
                    ? (await lastNameToProfessor(childText[2])).Match(val => val, ex => null)
                    : null;

                return new ISQEntryModel
                {
                    Course = course,
                    Season = season,
                    Year = year,
                    Crn = int.Parse(childText[1]),
                    Professor = professor,
                    NEnrolled = int.Parse(gpaText[3]),
                    NResponded = int.Parse(childText[4]),
                    Pct5 = double.Parse(childText[6]),
                    Pct4 = double.Parse(childText[7]),
                    Pct3 = double.Parse(childText[8]),
                    Pct2 = double.Parse(childText[9]),
                    Pct1 = double.Parse(childText[10]),
                    PctNa = double.Parse(childText[11]),
                    PctA = double.Parse(childText[4]),
                    PctAMinus = double.Parse(gpaText[5]),
                    PctBPlus = double.Parse(gpaText[6]),
                    PctB = double.Parse(gpaText[7]),
                    PctBMinus = double.Parse(gpaText[8]),
                    PctCPlus = double.Parse(gpaText[9]),
                    PctC = double.Parse(gpaText[10]),
                    PctD = double.Parse(gpaText[11]),
                    PctF = double.Parse(gpaText[12]),
                    PctWithdraw = double.Parse(gpaText[13]),
                    MeanGpa = double.Parse(gpaText[14])
                };
            })));
        }

        public static async Task<Try<ProfessorModel, IOException>> ScrapeDepartmentProfessor(
            string url,
            DepartmentModel dept
        )
        {
            var nNumberOpt = url.Capture(".*(N.*?)$");
            if (!nNumberOpt)
            {
                return new IOException($"Malformed url '{url}'. N-Number must be at end.");
            }

            var nNumber = nNumberOpt.Value;

            var document = await ToDocument(await Limiter.Run(() => Requests.Get(url)));
            if (!document.HasValue)
            {
                return new IOException($"Error while scraping NNumber '{nNumber}'.", document.Exception);
            }

            var tables = document.Value.QuerySelectorAll("table.datadisplaytable > tbody").ToList();

            if (tables.Count < 6)
            {
                return new IOException($"Not enough tables in the url '{url}'.");
            }

            if (tables[1].Children.Length < 1 || tables[1].Children.First().Children.Length < 2)
            {
                return new IOException($"Malformed page at '{url}'.");
            }

            var name = tables[1].Children[0].Children[1].InnerHtml.Split(" ");
            if (name.Length < 2)
            {
                return new IOException("Error while reading professor name.");
            }

            var fname = string.Join(" ", name.SkipLast(1));
            var lname = name.Last();

            return new ProfessorModel
                {Department = dept, FirstName = fname, LastName = lname, NNumber = nNumber};
        }

        /*
        public static async Task<Try<IEnumerable<ISQEntryModel>, IOException>>
            ScrapeDepartmentProfessorEntries(
                ProfessorModel professor,
                Func<string, Try<CourseModel>> courseCodeToCourse
            ) => await ScrapeDepartmentProfessorEntries(professor, (str, _) => Task.Run(() => courseCodeToCourse(str)));

        public static async Task<Try<IEnumerable<ISQEntryModel>, IOException>>
            ScrapeDepartmentProfessorEntries(
                ProfessorModel professor,
                Func<string, Term, Try<CourseModel>> courseCodeToCourse
            ) => await ScrapeDepartmentProfessorEntries(professor,
            (str, term) => Task.Run(() => courseCodeToCourse(str, term)));
        */

        public static async Task<Try<IEnumerable<Try<ISQEntryModel>>>>
            ScrapeDepartmentProfessorEntries(
                ProfessorModel professor,
                Func<string, CourseModel> courseCodeToCourse
            ) => await ScrapeDepartmentProfessorEntries(professor, courseCodeToCourse.ToAsync());

        public static async Task<Try<IEnumerable<Try<ISQEntryModel>>>>
            ScrapeDepartmentProfessorEntries(
                ProfessorModel professor,
                Func<string, Term, CourseModel> courseCodeToCourse
            ) => await ScrapeDepartmentProfessorEntries(professor, courseCodeToCourse.ToAsync());

        public static async Task<Try<IEnumerable<Try<ISQEntryModel>>>>
            ScrapeDepartmentProfessorEntries(
                ProfessorModel professor,
                Func<string, Try<CourseModel>> courseCodeToCourse
            ) => await ScrapeDepartmentProfessorEntries(professor, courseCodeToCourse.ToAsync());

        public static async Task<Try<IEnumerable<Try<ISQEntryModel>>>>
            ScrapeDepartmentProfessorEntries(
                ProfessorModel professor,
                Func<string, Term, Try<CourseModel>> courseCodeToCourse
            ) => await ScrapeDepartmentProfessorEntries(professor, courseCodeToCourse.ToAsync());

        public static async Task<Try<IEnumerable<Try<ISQEntryModel>>>>
            ScrapeDepartmentProfessorEntries(
                ProfessorModel professor,
                Func<string, Task<CourseModel>> courseCodeToCourse
            ) => await ScrapeDepartmentProfessorEntries(professor,
            async (str, _) => new Try<CourseModel>(await courseCodeToCourse(str)));

        public static async Task<Try<IEnumerable<Try<ISQEntryModel>>>>
            ScrapeDepartmentProfessorEntries(
                ProfessorModel professor,
                Func<string, Term, Task<CourseModel>> courseCodeToCourse
            ) => await ScrapeDepartmentProfessorEntries(professor,
            async (str, term) => new Try<CourseModel>(await courseCodeToCourse(str, term)));

        public static async Task<Try<IEnumerable<Try<ISQEntryModel>>>>
            ScrapeDepartmentProfessorEntries(
                ProfessorModel professor,
                Func<string, Task<Try<CourseModel>>> courseCodeToCourse
            ) => await ScrapeDepartmentProfessorEntries(professor, (str, _) => courseCodeToCourse(str));

        public static async Task<Try<IEnumerable<Try<ISQEntryModel>>>>
            ScrapeDepartmentProfessorEntries(
                ProfessorModel professor,
                Func<string, Term, Task<Try<CourseModel>>> courseCodeToCourse
            )
        {
            var url =
                $"https://bannerssb.unf.edu/nfpo-ssb/wksfwbs.p_instructor_isq_grade?pv_instructor={professor.NNumber}";

            var document = await ToDocument(await Limiter.Run(() => Requests.Get(url)));
            if (!document)
            {
                return new IOException($"Error while scraping NNumber '{professor.NNumber}'.", document.Exception);
            }

            var tables = document.Value.QuerySelectorAll("table.datadisplaytable > tbody").ToList();

            var isqTable = tables[3];
            var gpaTable = tables[5];

            return await ProfessorTablesToEntries(professor, isqTable, gpaTable, courseCodeToCourse);
        }

        private static async Task<Try<IEnumerable<Try<ISQEntryModel>>, MalformedPageException>>
            ProfessorTablesToEntries(ProfessorModel professor,
                IElement childTable,
                IElement gpaTable, Func<string, Term, Task<Try<CourseModel>>> courseCodeToCourse)
        {
            var childTableChildren = childTable.Children.ToList();
            var gpaTableChildren = gpaTable.Children.ToList();

            if (childTableChildren.Count < 3 || gpaTableChildren.Count < 3)
            {
                return new MalformedPageException(
                    $"Expected at least 3 rows in both child table and GPA table. Got {childTableChildren.Count} in child table and {gpaTableChildren.Count} in GPA table.");
            }

            // the top 2 rows on either table is heading, so we skip it
            // for each cell of each row, get the inner html if it doesn't have any children (plain text), otherwise get the innerhtml of the first child (link)
            var childRows = childTable.Children.Skip(2).Select(x => x.Children.Select(y =>
                y.Children.Length == 0 ? y.InnerHtml.Trim() : y.Children.First().InnerHtml.Trim()).ToList()).ToList();
            var gpaRows = gpaTable.Children.Skip(2).Select(x => x.Children.Select(y =>
                y.Children.Length == 0 ? y.InnerHtml.Trim() : y.Children.First().InnerHtml.Trim()).ToList()).ToList();

            var crnToChildRows = childRows.GroupBy(x => x[1]);
            var gpaToChildRows = gpaRows.GroupBy(x => x[1]);

            var crnToChildRow = crnToChildRows.ToDictionary(x => x.Key, x => x.First());
            var gpaToChildRow = gpaToChildRows.ToDictionary(x => x.Key, x => x.First());

            return await Task.WhenAll(crnToChildRow
                .Join(gpaToChildRow,
                    x => x.Key,
                    y => y.Key,
                    (x, y) => (x.Value, y.Value)
                )
                .Select(async x =>
                {
                    var (childText, gpaText) = x;
                    var term = new Term(childText[0]);

                    var courseTry = await courseCodeToCourse(childText[2], term);

                    if (!courseTry)
                    {
                        return courseTry.Exception;
                    }

                    var course = courseTry.Value;

                    var entry = Try.Of(() => new ISQEntryModel
                    {
                        Season = term.Season,
                        Year = term.Year,
                        Course = course,
                        Crn = Parse.Int(childText[1]).Value,
                        Professor = professor,
                        NEnrolled = Parse.Int(gpaText[3]).Value,
                        NResponded = Parse.Int(childText[4]).Value,
                        Pct5 = Parse.Double(childText[6]).Value,
                        Pct4 = Parse.Double(childText[7]).Value,
                        Pct3 = Parse.Double(childText[8]).Value,
                        Pct2 = Parse.Double(childText[9]).Value,
                        Pct1 = Parse.Double(childText[10]).Value,
                        PctNa = Parse.Double(childText[11]).Value,
                        PctA = Parse.Double(gpaText[4]).Value,
                        PctAMinus = Parse.Double(gpaText[5]).Value,
                        PctBPlus = Parse.Double(gpaText[6]).Value,
                        PctB = Parse.Double(gpaText[7]).Value,
                        PctBMinus = Parse.Double(gpaText[8]).Value,
                        PctCPlus = Parse.Double(gpaText[9]).Value,
                        PctC = Parse.Double(gpaText[10]).Value,
                        PctD = Parse.Double(gpaText[11]).Value,
                        PctF = Parse.Double(gpaText[12]).Value,
                        PctWithdraw = Parse.Double(gpaText[13]).Value,
                        MeanGpa = gpaText[14].Trim() == "" ? 0.0 : Parse.Double(gpaText[14]).Value
                    });

                    if (!entry)
                    {
                        return new MalformedPageException(
                            $"Failed to parse row, Child: '{childText.Join("\t")}', GPA:'{gpaText.Join("\t")}'",
                            entry.Exception);
                    }

                    return Try.Of(entry.Value);
                }));
        }

        public static Func<TKey, Try<TValue>> ToFunc<TKey, TValue>(
            this IDictionary<TKey, TValue> dict)
            where TValue : class =>
            key => Try.Of(dict.ContainsKey(key), () => dict[key],
                () => new ArgumentException($"Key '{key}' not found in dictionary."));

        public static Func<TParam, Task<TRes>> ToAsync<TParam, TRes>(this Func<TParam, TRes> func) =>
            param => Task.Run(() => func(param));

        public static Func<TParam, TParam2, Task<TRes>> ToAsync<TParam, TParam2, TRes>(
            this Func<TParam, TParam2, TRes> func) =>
            (param, param2) => Task.Run(() => func(param, param2));

        public static async Task<Try<
            (
            (
            IEnumerable<DepartmentModel> Succeeded,
            IEnumerable<DepartmentScrapeException> Failed
            ) Departments,
            (
            IEnumerable<CourseModel> Succeeded,
            IEnumerable<CourseScrapeException> Failed
            ) Courses,
            (
            IEnumerable<ProfessorModel> Succeeded,
            IEnumerable<ProfessorScrapeException> Failed
            ) Professors,
            IEnumerable<ISQEntryModel> Entries,
            IEnumerable<Exception> Errors
            )>> ScrapeAll()
        {
            var deptsFailed = new ConcurrentSet<DepartmentScrapeException>();
            var coursesFailed = new ConcurrentSet<CourseScrapeException>();
            var professors = new ConcurrentSet<ProfessorModel>();
            var professorsFailed = new ConcurrentSet<ProfessorScrapeException>();
            var entries = new ConcurrentSet<ISQEntryModel>();
            var errors = new ConcurrentBag<Exception>();

            var depts = await ScrapeDepartmentIds();
            if (!depts)
            {
                return depts.Exception;
            }

            var courseCodeToCourse =
                new ConcurrentDictionary<string, CourseModel>();

            async Task<Optional<Exception>> ScrapeAllRec(DepartmentModel dept, Term when)
            {
                var deptRes = await ScrapeDepartment(dept, when);

                if (!deptRes)
                {
                    return new DepartmentScrapeException(dept, "Failed to scrape the department.", deptRes.Exception);
                }

                deptRes.Value.Courses.ForEach(course => courseCodeToCourse[course.CourseCode] = course);
                deptRes.Value.Exceptions.ForEach(ex => errors.Add(ex));

                var res = await deptRes.Value.Professors.TryAllParallel(async prof =>
                {
                    if (professors.Contains(prof))
                    {
                        return null;
                    }

                    professors.Add(prof);

                    var profEntries =
                        await ScrapeDepartmentProfessorEntries(prof, code => new CourseModel {CourseCode = code});

                    if (!profEntries)
                    {
                        if (profEntries.Exception is IOException ioex)
                        {
                            return ioex;
                        }

                        errors.Add(profEntries.Exception);
                    }
                    else
                    {
                        profEntries.Value.ForEach(entry => entry.Match(
                            val => entries.Add(entry.Value),
                            ex => errors.Add(ex))
                        );
                    }

                    return null;
                });

                return res.Match(val => val, () => null);
            }

            var currentTerm = new Term() - 1;
            var tasks = depts.Value.Select(dept => ScrapeAllRec(dept, currentTerm));
            var resultTask = Task.WhenAll(tasks);
            try
            {
                var tmp = await resultTask;
                foreach (var res in tmp)
                {
                    if (res && res.Value is IOException ioex)
                    {
                        return ioex;
                    }

                    res.Match(ex => errors.Add(ex));
                }

                foreach (var entry in entries)
                {
                    if (courseCodeToCourse.ContainsKey(entry.Course.CourseCode))
                    {
                        entry.Course = courseCodeToCourse[entry.Course.CourseCode];
                    }
                    else
                    {
                        coursesFailed.Add(
                            new CourseScrapeException(entry.Course.CourseCode, "This course code was not found."));
                    }
                }

                return (
                    (depts.Value, deptsFailed),
                    (courseCodeToCourse.Values, coursesFailed),
                    (professors, professorsFailed),
                    entries,
                    errors
                );
            }
            catch (Exception e)
            {
                return e.Cast<IOException>("Failed to scrape data.");
            }
        }
    }
}