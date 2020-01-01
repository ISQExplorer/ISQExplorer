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

        public static async Task<ITry<IDocument, IOException>> ToDocument(ITry<string, IOException> html)
        {
            return html.HasValue ? new Try<IDocument, IOException>(await ToDocument(html.Value)) : html.Exception;
        }

        /// <summary>
        /// Retrieves a list of departments from the web.
        /// </summary>
        /// <returns>A Try containing a list of departments, or an IOException detailing why it could not be returned.</returns>
        /// <exception cref="IOException">Details the type of error.</exception>
        public static async Task<ITry<IEnumerable<DepartmentModel>, IOException>> ScrapeDepartmentIds()
        {
            const string url = "https://banner.unf.edu/pls/nfpo/wksfwbs.p_dept_schd";
            var html = await Requests.Get(url);
            if (!html)
            {
                return new Try<IEnumerable<DepartmentModel>, IOException>(
                    new IOException("Error while retrieving departments.", html.Exception));
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
                        throw new IOException(
                            $"Non option element found as child of selector with OuterHTML '{x.OuterHtml}'.");
                    }

                    var id = Parse.Int(opt.Value);
                    var label = opt.Label;

                    if (!id)
                    {
                        throw new IOException($"Invalid id found '{opt.Value}'.");
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
        /// <param name="onCourse">A function that receives a <see cref="CourseModel"/> and handles it. This function must not throw.</param>
        /// <param name="onProfessor">A function that receives a <see cref="ProfessorModel"/> and handles it. This function must not throw.</param>
        /// <param name="logger">A logger that will receive any warnings/errors produced while scraping the department.</param>
        /// <returns>An Optional containing an IOException on failure, or nothing on success.</returns>
        public static async Task<Optional<IOException>> ScrapeDepartment(
            DepartmentModel dept,
            Term? when = null,
            Action<CourseModel>? onCourse = null,
            Action<ProfessorModel>? onProfessor = null,
            ILogger? logger = null
        ) => await ScrapeDepartment(dept, when,
            course => Task.Run(() =>
            {
                onCourse?.Invoke(course);
                return new Optional<Exception>();
            }),
            professor => Task.Run(() =>
            {
                onProfessor?.Invoke(professor);
                return new Optional<Exception>();
            })
        );

        /// <summary>
        /// Scrapes courses and professors from a department.
        /// Use <see cref="ScrapeDepartmentIds"/> to get a list of departments.
        /// </summary>
        /// <param name="dept">The department to scrape.</param>
        /// <param name="when">The Term to scrape entries from. By default this is one semester before the current one
        /// So if it is currently Fall 2019, this function would scrape entries from Summer 2019.</param>
        /// <param name="onCourse">A function that receives a <see cref="CourseModel"/> and handles it. This function must not throw.</param>
        /// <param name="onProfessor">A function that receives a <see cref="ProfessorModel"/> and handles it. This function must not throw.</param>
        /// <param name="logger">A logger that will receive any warnings/errors produced while scraping the department.</param>
        /// <returns>An Optional containing an IOException on failure, or nothing on success.</returns>
        public static async Task<Optional<IOException>> ScrapeDepartment(
            DepartmentModel dept,
            Term? when = null,
            Func<CourseModel, Task<Optional<Exception>>>? onCourse = null,
            Func<ProfessorModel, Task<Optional<Exception>>>? onProfessor = null,
            ILogger? logger = null
        )
        {
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
                        logger?.LogInformation(
                            $"Malformed row. Incorrect number of columns in table. Expected 20, got {children.Count}");
                        return null;
                    }

                    if (WebUtility.HtmlDecode(children.First().InnerHtml)?.Trim() != "")
                    {
                        return null;
                    }

                    if (onCourse != null)
                    {
                        var courseRes = await onCourse(new CourseModel
                        {
                            CourseCode = children[2].Children.First().InnerHtml,
                            Department = dept,
                            Name = children[3].InnerHtml
                        });

                        if (courseRes != null && courseRes.HasValue)
                        {
                            return courseRes.Value;
                        }
                    }

                    if (children[17].Children.None() ||
                        !(children[17].Children.First() is IHtmlAnchorElement professorCell))
                    {
                        logger?.LogWarning("Expected anchor element in 17th column, did not get one.");
                        return null;
                    }

                    var url = $"https://banner.unf.edu/pls/nfpo{professorCell.PathName}{professorCell.Search}";

                    if (!urlToProf.ContainsKey(url))
                    {
                        var profTry = await ScrapeDepartmentProfessor(url, dept);

                        if (!profTry)
                        {
                            logger?.LogWarning(profTry.Exception.ToString());
                            return null;
                        }

                        urlToProf[url] = profTry.Value;
                    }

                    if (onProfessor != null)
                    {
                        var profRes = await onProfessor(urlToProf[url]);

                        if (profRes != null && profRes.HasValue)
                        {
                            return profRes.Value;
                        }
                    }

                    return null;
                });

            return res.Select(e =>
                e is IOException exception
                    ? exception
                    : new IOException("An error occured while scraping the department.", e));
        }


        public static async Task<ITry<IEnumerable<ISQEntryModel>, IOException>> ScrapeDepartmentCourseEntries(
            CourseModel course,
            Func<string, ITry<ProfessorModel>> lastNameToProfessor
        ) => await ScrapeDepartmentCourseEntries(course, lastNameToProfessor.ToAsync());


        public static async Task<ITry<IEnumerable<ISQEntryModel>, IOException>> ScrapeDepartmentCourseEntries(
            CourseModel course,
            Func<string, Task<ITry<ProfessorModel>>> lastNameToProfessor
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

        public static async Task<ITry<ProfessorModel, IOException>> ScrapeDepartmentProfessor(
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

        public static async Task<ITry<IEnumerable<ISQEntryModel>, IOException>>
            ScrapeDepartmentProfessorEntries(
                ProfessorModel professor,
                Func<string, CourseModel> courseCodeToCourse
            ) => await ScrapeDepartmentProfessorEntries(professor, courseCodeToCourse.ToAsync());

        public static async Task<ITry<IEnumerable<ISQEntryModel>, IOException>>
            ScrapeDepartmentProfessorEntries(
                ProfessorModel professor,
                Func<string, Term, CourseModel> courseCodeToCourse
            ) => await ScrapeDepartmentProfessorEntries(professor, courseCodeToCourse.ToAsync());

        public static async Task<ITry<IEnumerable<ISQEntryModel>, IOException>>
            ScrapeDepartmentProfessorEntries(
                ProfessorModel professor,
                Func<string, ITry<CourseModel>> courseCodeToCourse
            ) => await ScrapeDepartmentProfessorEntries(professor, courseCodeToCourse.ToAsync());

        public static async Task<ITry<IEnumerable<ISQEntryModel>, IOException>>
            ScrapeDepartmentProfessorEntries(
                ProfessorModel professor,
                Func<string, Term, ITry<CourseModel>> courseCodeToCourse
            ) => await ScrapeDepartmentProfessorEntries(professor, courseCodeToCourse.ToAsync());

        public static async Task<ITry<IEnumerable<ISQEntryModel>, IOException>>
            ScrapeDepartmentProfessorEntries(
                ProfessorModel professor,
                Func<string, Task<CourseModel>> courseCodeToCourse
            ) => await ScrapeDepartmentProfessorEntries(professor,
            async (str, _) => new Try<CourseModel>(await courseCodeToCourse(str)));

        public static async Task<ITry<IEnumerable<ISQEntryModel>, IOException>>
            ScrapeDepartmentProfessorEntries(
                ProfessorModel professor,
                Func<string, Term, Task<CourseModel>> courseCodeToCourse
            ) => await ScrapeDepartmentProfessorEntries(professor,
            async (str, term) => new Try<CourseModel>(await courseCodeToCourse(str, term)));

        public static async Task<ITry<IEnumerable<ISQEntryModel>, IOException>>
            ScrapeDepartmentProfessorEntries(
                ProfessorModel professor,
                Func<string, Task<ITry<CourseModel>>> courseCodeToCourse
            ) => await ScrapeDepartmentProfessorEntries(professor, (str, _) => courseCodeToCourse(str));

        public static async Task<ITry<IEnumerable<ISQEntryModel>, IOException>>
            ScrapeDepartmentProfessorEntries(
                ProfessorModel professor,
                Func<string, Term, Task<ITry<CourseModel>>> courseCodeToCourse
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
            IElement gpaTable, Func<string, Term, Task<ITry<CourseModel>>> courseCodeToCourse)
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

        public static Func<TKey, ITry<TValue, ArgumentException>> ToFunc<TKey, TValue>(
            this IDictionary<TKey, TValue> dict)
            where TValue : class =>
            key => Try.Of(dict.ContainsKey(key), dict[key],
                new ArgumentException($"Key '{key}' not found in dictionary."));

        public static Func<TParam, Task<TRes>> ToAsync<TParam, TRes>(this Func<TParam, TRes> func) =>
            param => Task.Run(() => func(param));

        public static Func<TParam, TParam2, Task<TRes>> ToAsync<TParam, TParam2, TRes>(
            this Func<TParam, TParam2, TRes> func) =>
            (param, param2) => Task.Run(() => func(param, param2));

        public static async Task<Optional<IOException>> ScrapeAll(ConcurrentSet<CourseModel> courses,
            ConcurrentSet<ProfessorModel> professors)
        {
            var depts = await ScrapeDepartmentIds();
            if (!depts)
            {
                return depts.Exception;
            }

            var courseCodeToCourse =
                new ConcurrentDictionary<string, CourseModel>(courses.ToDictionary(c => c.CourseCode, c => c));

            async Task<Optional<IOException>> ScrapeAllRec(DepartmentModel dept, Term when)
            {
                var res = await ScrapeDepartment(
                    dept,
                    when,
                    course =>
                    {
                        courses.Add(course);
                        courseCodeToCourse[course.CourseCode] = course;
                        return null;
                    },
                    async professor =>
                    {
                        professors.Add(professor);
                        var profEntries = await ScrapeDepartmentProfessorEntries(professor, async (code, term) =>
                        {
                            if (!courseCodeToCourse.ContainsKey(code))
                            {
                                var courseRes = await ScrapeAllRec(dept, term);
                                if (courseRes)
                                {
                                    return new Try<CourseModel>(courseRes.Value);
                                }
                            }

                            return new Try<CourseModel>(courseCodeToCourse[code]);
                        });
                        return null;
                    }
                );

                return null;
            }

            return null;
        }
    }
}