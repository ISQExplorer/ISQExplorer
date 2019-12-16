#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using ISQExplorer.Functional;
using ISQExplorer.Misc;
using ISQExplorer.Models;
using ISQExplorer.Repositories;
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
            Action<ProfessorModel>? onProfessor = null
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

            document.Value
                .QuerySelectorAll("table.datadisplaytable > tbody > tr")
                .Skip(3)
                .ForEach(x =>
                {
                    var children = x.Children.ToList();

                    onCourse?.Invoke(new CourseModel
                    {
                        CourseCode = children[2].Children.First().InnerHtml,
                        Department = dept,
                        Name = children[3].InnerHtml
                    });

                    var professorCell = children[16];

                    onProfessor?.Invoke(new ProfessorModel
                    {
                        Department = dept,
                    });
                });

            throw new NotImplementedException();
        }

        private static async Task<Try<IEnumerable<ISQEntryModel>, IOException>> ScrapeDepartmentCourse(
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

        public static async Task<Try<(ProfessorModel Professor, IEnumerable<ISQEntryModel> Entries), IOException>>
            ScrapeDepartmentProfessor(
                string nNumber,
                DepartmentModel dept,
                Func<string, Task<CourseModel>> courseCodeToCourse
            )
        {
            var url =
                $"https://banner.unf.edu/pls/nfpo/wksfwbs.p_instructor_isq_grade?pv_instructor={nNumber}";

            var document = await ToDocument(await Requests.Get(url));
            if (!document)
            {
                return new IOException($"Error while scraping NNumber '{nNumber}'.", document.Exception);
            }

            var tables = document.Value.QuerySelectorAll("table.datadisplaytable > tbody").ToList();


            var name = tables[1].Children[0].Children[1].InnerHtml.Split(" ");
            if (name.Length < 2)
            {
                return new IOException("Error while reading professor name.");
            }

            var fname = string.Join(" ", name.SkipLast(1));
            var lname = name.Last();

            var prof = new ProfessorModel
                {Department = dept, FirstName = fname, LastName = lname, NNumber = nNumber};

            var entries = await ProfessorTablesToEntries(prof, tables[3], tables[5], courseCodeToCourse);

            return (prof, entries);
        }

        public static async Task<Try<IEnumerable<ISQEntryModel>, IOException>>
            ScrapeDepartmentProfessor(
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