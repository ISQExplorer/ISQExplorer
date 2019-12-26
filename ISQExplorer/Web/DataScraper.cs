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

        public static async Task<Try<IDocument, IOException>> ToDocument(Try<string, IOException> html)
        {
            return html ? new Try<IDocument, IOException>(await ToDocument(html.Value)) : html.Exception;
        }

        public static async Task<Try<IEnumerable<DepartmentModel>, IOException>> ScrapeDepartmentIds()
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

        public static async Task<Optional<IOException>> ScrapeDepartment(
            DepartmentModel dept,
            Term? when = null,
            Action<CourseModel>? onCourse = null,
            Action<ProfessorModel>? onProfessor = null,
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
            if (!document)
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
                        return;
                    }

                    if (WebUtility.HtmlDecode(children.First().InnerHtml)?.Trim() != "")
                    {
                        return;
                    }

                    onCourse?.Invoke(new CourseModel
                    {
                        CourseCode = children[2].Children.First().InnerHtml,
                        Department = dept,
                        Name = children[3].InnerHtml
                    });

                    if (children[17].Children.None() ||
                        !(children[17].Children.First() is IHtmlAnchorElement professorCell))
                    {
                        logger?.LogWarning("Expected anchor element in 17th column, did not get one.");
                        return;
                    }

                    var url = $"https://banner.unf.edu/pls/nfpo{professorCell.PathName}{professorCell.Search}";

                    if (!urlToProf.ContainsKey(url))
                    {
                        var profTry = await ScrapeDepartmentProfessor(url, dept);

                        if (!profTry)
                        {
                            logger?.LogWarning(profTry.Exception.ToString());
                            return;
                        }

                        urlToProf[url] = profTry.Value;
                    }

                    onProfessor?.Invoke(urlToProf[url]);
                });

            return res.Select(e => new IOException("An error occured while scraping the department.", e));
        }

        public static async Task<Try<IEnumerable<ISQEntryModel>, IOException>> ScrapeDepartmentCourseEntries(
            CourseModel course,
            Func<string, Task<ProfessorModel?>>? lastNameToProfessor = null
        )
        {
            var url =
                $"https://banner.unf.edu/pls/nfpo/wksfwbs.p_course_isq_grade?pv_course_id={course.CourseCode}";
            var document = await ToDocument(await Requests.Get(url));
            if (!document)
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
                var professor = lastNameToProfessor != null ? await lastNameToProfessor(childText[2]) : null;

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

        public static async Task<Try<ProfessorModel, IOException>>
            ScrapeDepartmentProfessor(
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
            if (!document)
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

        public static async Task<Try<IEnumerable<ISQEntryModel>, IOException>>
            ScrapeDepartmentProfessorEntries(
                ProfessorModel professor,
                Func<string, CourseModel?> courseCodeToCourse
            ) => await ScrapeDepartmentProfessorEntries(professor, str => Task.Run(() => courseCodeToCourse(str)));

        public static async Task<Try<IEnumerable<ISQEntryModel>, IOException>>
            ScrapeDepartmentProfessorEntries(
                ProfessorModel professor,
                Func<string, Task<CourseModel?>> courseCodeToCourse
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
            IElement gpaTable, Func<string, Task<CourseModel?>> courseCodeToCourse)
        {
            return await Task.WhenAll(childTable.Children.Skip(2).Zip(gpaTable.Children.Skip(2)).Select(async x =>
            {
                var (isq, gpa) = x;
                var childText = isq.Children.Select(y =>
                    y.Children.Length == 0 ? y.InnerHtml.Trim() : y.Children.First().InnerHtml.Trim()).ToList();
                var gpaText = gpa.Children.Select(y =>
                    y.Children.Length == 0 ? y.InnerHtml.Trim() : y.Children.First().InnerHtml.Trim()).ToList();
                var term = new Term(childText[0]);

                var course = await courseCodeToCourse(childText[2]);

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
    }
}