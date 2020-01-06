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
using Microsoft.Extensions.Logging;

namespace ISQExplorer.Web
{
    public static class DataScraper
    {
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

        /// <summary>
        /// Retrieves a list of departments from the web.
        /// </summary>
        /// <returns>A Try containing a list of departments, or an IOException detailing why it could not be returned.</returns>
        /// <exception cref="IOException">Details the type of error.</exception>
        public static async Task<Try<IEnumerable<DepartmentModel>>> ScrapeDepartmentIds()
        {
            const string url = "https://banner.unf.edu/pls/nfpo/wksfwbs.p_dept_schd";
            var html = await Requests.Get(url);
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

            var document = await ToDocument(await Requests.Post("https://banner.unf.edu/pls/nfpo/wksfwbs.p_dept_schd",
                $"pv_term={no}&pv_dept={dept.Id}&pv_ptrm=&pv_campus=&pv_sub=Submit"));
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
                    var children = x.Children.ToList();

                    if (children.Count != 20)
                    {
                        exceptions.Add(new MalformedPageException(
                            $"Malformed row. Incorrect number of columns in table. Expected 20, got {children.Count}"));
                        return;
                    }

                    if (WebUtility.HtmlDecode(children.First().InnerHtml)?.Trim() != "")
                    {
                        return;
                    }

                    courses.Add(new CourseModel
                    {
                        CourseCode = children[2].Children.First().InnerHtml,
                        Department = dept,
                        Name = children[3].InnerHtml
                    });

                    if (children[17].Children.None() ||
                        !(children[17].Children.First() is IHtmlAnchorElement professorCell))
                    {
                        exceptions.Add(
                            new MalformedPageException($"Expected anchor element in 17th column, did not get one."));
                        return;
                    }

                    var url = $"https://banner.unf.edu/pls/nfpo{professorCell.PathName}{professorCell.Search}";

                    if (!urlToProf.ContainsKey(url))
                    {
                        var profTry = await ScrapeDepartmentProfessor(url, dept);

                        if (!profTry)
                        {
                            exceptions.Add(new MalformedPageException("Failed to scrape professor data",
                                profTry.Exception));
                            return;
                        }

                        urlToProf[url] = profTry.Value;
                    }

                    professors.Add(urlToProf[url]);
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
                $"https://banner.unf.edu/pls/nfpo/wksfwbs.p_course_isq_grade?pv_course_id={course.CourseCode}";
            var document = await ToDocument(await Requests.Get(url));
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

            var document = await ToDocument(await Requests.Get(url));
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

        public static async Task<Try<IEnumerable<ISQEntryModel>, IOException>>
            ScrapeDepartmentProfessorEntries(
                ProfessorModel professor,
                Func<string, CourseModel> courseCodeToCourse
            ) => await ScrapeDepartmentProfessorEntries(professor, courseCodeToCourse.ToAsync());

        public static async Task<Try<IEnumerable<ISQEntryModel>, IOException>>
            ScrapeDepartmentProfessorEntries(
                ProfessorModel professor,
                Func<string, Term, CourseModel> courseCodeToCourse
            ) => await ScrapeDepartmentProfessorEntries(professor, courseCodeToCourse.ToAsync());

        public static async Task<Try<IEnumerable<ISQEntryModel>, IOException>>
            ScrapeDepartmentProfessorEntries(
                ProfessorModel professor,
                Func<string, Try<CourseModel>> courseCodeToCourse
            ) => await ScrapeDepartmentProfessorEntries(professor, courseCodeToCourse.ToAsync());

        public static async Task<Try<IEnumerable<ISQEntryModel>, IOException>>
            ScrapeDepartmentProfessorEntries(
                ProfessorModel professor,
                Func<string, Term, Try<CourseModel>> courseCodeToCourse
            ) => await ScrapeDepartmentProfessorEntries(professor, courseCodeToCourse.ToAsync());

        public static async Task<Try<IEnumerable<ISQEntryModel>, IOException>>
            ScrapeDepartmentProfessorEntries(
                ProfessorModel professor,
                Func<string, Task<CourseModel>> courseCodeToCourse
            ) => await ScrapeDepartmentProfessorEntries(professor,
            async (str, _) => new Try<CourseModel>(await courseCodeToCourse(str)));

        public static async Task<Try<IEnumerable<ISQEntryModel>, IOException>>
            ScrapeDepartmentProfessorEntries(
                ProfessorModel professor,
                Func<string, Term, Task<CourseModel>> courseCodeToCourse
            ) => await ScrapeDepartmentProfessorEntries(professor,
            async (str, term) => new Try<CourseModel>(await courseCodeToCourse(str, term)));

        public static async Task<Try<IEnumerable<ISQEntryModel>, IOException>>
            ScrapeDepartmentProfessorEntries(
                ProfessorModel professor,
                Func<string, Task<Try<CourseModel>>> courseCodeToCourse
            ) => await ScrapeDepartmentProfessorEntries(professor, (str, _) => courseCodeToCourse(str));

        public static async Task<Try<IEnumerable<ISQEntryModel>, IOException>>
            ScrapeDepartmentProfessorEntries(
                ProfessorModel professor,
                Func<string, Term, Task<Try<CourseModel>>> courseCodeToCourse
            )
        {
            var url =
                $"https://banner.unf.edu/pls/nfpo/wksfwbs.p_instructor_isq_grade?pv_instructor={professor.NNumber}";

            var document = await ToDocument(await Requests.Get(url));
            if (!document)
            {
                return new IOException($"Error while scraping NNumber '{professor.NNumber}'.", document.Exception);
            }

            var tables = document.Value.QuerySelectorAll("table.datadisplaytable > tbody").ToList();

            var isqTable = tables[3];
            var gpaTable = tables[5];

            return new Try<IEnumerable<ISQEntryModel>, IOException>(
                await ProfessorTablesToEntries(professor, isqTable, gpaTable, courseCodeToCourse));
        }

        private static async Task<IEnumerable<ISQEntryModel>> ProfessorTablesToEntries(ProfessorModel professor,
            IElement childTable,
            IElement gpaTable, Func<string, Term, Task<Try<CourseModel>>> courseCodeToCourse)
        {
            return await Task.WhenAll(childTable.Children.Skip(2).Zip(gpaTable.Children.Skip(2)).Select(async x =>
            {
                var (isq, gpa) = x;
                var childText = isq.Children.Select(y =>
                    y.Children.Length == 0 ? y.InnerHtml.Trim() : y.Children.First().InnerHtml.Trim()).ToList();
                var gpaText = gpa.Children.Select(y =>
                    y.Children.Length == 0 ? y.InnerHtml.Trim() : y.Children.First().InnerHtml.Trim()).ToList();
                var term = new Term(childText[0]);

                var courseTry = await courseCodeToCourse(childText[2], term);
                var course = courseTry.HasValue ? courseTry.Value : null;

                return new ISQEntryModel
                {
                    Season = term.Season,
                    Year = term.Year,
                    Course = course,
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
            }));
        }

        public static Func<TKey, Try<TValue>> ToFunc<TKey, TValue>(
            this IDictionary<TKey, TValue> dict)
            where TValue : class =>
            key => Try.Of(dict.ContainsKey(key), dict[key],
                new ArgumentException($"Key '{key}' not found in dictionary."));

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
            var tasks = new ConcurrentDictionary<(DepartmentModel, Term), Task<Optional<IOException>>>();
            var taskLock = new object();
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

            async Task<Optional<IOException>> ScrapeAllRec(DepartmentModel dept, Term when)
            {
                var deptRes = await ScrapeDepartment(dept, when);

                if (!deptRes)
                {
                    return new IOException("Failed to scrape the department.", deptRes.Exception);
                }

                deptRes.Value.Courses.ForEach(course => courseCodeToCourse[course.CourseCode] = course);
                deptRes.Value.Exceptions.ForEach(ex => errors.Add(ex));

                deptRes.Value.Professors.AsParallel().ForEach(async prof =>
                {
                    if (professors.Contains(prof))
                    {
                        return;
                    }

                    professors.Add(prof);

                    var profEntries = await ScrapeDepartmentProfessorEntries(prof, async (code, term) =>
                    {
                        if (!courseCodeToCourse.ContainsKey(code))
                        {
                            lock (taskLock)
                            {
                                if (!tasks.ContainsKey((dept, term)))
                                {
                                    tasks[(dept, term)] = ScrapeAllRec(dept, term);
                                }
                            }

                            var courseRes = await tasks[(dept, term)];

                            if (courseRes)
                            {
                                errors.Add(courseRes.Value);
                                return courseRes.Value;
                            }

                            if (!courseCodeToCourse.ContainsKey(code))
                            {
                                var ex = new CourseScrapeException(code,
                                    $"Failed to scrape course code from expected term {term}.");
                                coursesFailed.Add(ex);
                                return ex;
                            }
                        }

                        return courseCodeToCourse[code];
                    });

                    if (!profEntries)
                    {
                        errors.Add(profEntries.Exception);
                    }
                    else
                    {
                        profEntries.Value.ForEach(entry => entries.Add(entry));
                    }
                });

                return null;
            }

            var currentTerm = new Term() - 1;
            depts.Value.AsParallel().ForEach(dept => tasks[(dept, currentTerm)] = ScrapeAllRec(dept, currentTerm));
            var resultTask = Task.WhenAll(tasks.Select(time => time.Value));
            try
            {
                var tmp = await resultTask;
                tmp.Where(x => x).ForEach(x => errors.Add(x.Value));

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