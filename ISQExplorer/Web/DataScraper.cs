using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using ISQExplorer.Functional;
using ISQExplorer.Misc;
using ISQExplorer.Models;

namespace ISQExplorer.Web
{
    public class DataScraper
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

        public async Task<Try<DepartmentModel, IOException>> GetDepartments()
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

            throw new NotImplementedException();
        }

        public async Task<Optional<IOException>> ScrapeDepartment(
            int dept,
            ConcurrentDictionary<string, ProfessorModel> professors,
            ConcurrentDictionary<string, CourseModel> courses,
            Term? when = null
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
                $"pv_term={no}&pv_dept={dept}&pv_ptrm=&pv_campus=&pv_sub=Submit"));
            if (!document)
            {
                return new IOException($"Error while retrieving department page {dept}.", document.Exception);
            }

            throw new NotImplementedException();
        }

        private async Task<Try<IEnumerable<ISQEntryModel>, IOException>> ScrapeCourseCode(
            CourseModel course,
            ConcurrentDictionary<string, ProfessorModel> professors
        )
        {
            var url =
                $"https://banner.unf.edu/pls/nfpo/wksfwbs.p_course_isq_grade?pv_course_id={course.CourseCode.ToUpper()}";
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
                var professor = professors.ContainsKey(childText[2]) ? professors[childText[2]] : null;

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

        public async Task<Try<IEnumerable<ISQEntryModel>, IOException>> ScrapeProfessor(
            string nNumber,
            ConcurrentDictionary<string, ProfessorModel> professors,
            ConcurrentDictionary<string, CourseModel> courses
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
            
            if (!professors.ContainsKey(nNumber))
            {
                var name = tables[1].Children[0].Children[1].InnerHtml.Split(" ");
                if (name.Length < 2)
                {
                    return new IOException("Error while reading professor name.");
                }
                
                var fname = name.SkipLast(1).Join(" ");
                var lname = name.Last();
            }

            var isqTable = tables[3];
            var gpaTable = tables[5];

            ProfessorModel professor;
            try
            {
                professor = _context.Professors.First(x => x.NNumber.Equals(nNumber.ToUpper()));
            }
            catch (InvalidOperationException)
            {
                _logger.LogWarning($"No professor found in database with N-Number '{nNumber}'.");
                return new ISQEntryModel[0];
            }

            return await Task.WhenAll(isqTable.Children.Skip(2).Zip(gpaTable.Children.Skip(2)).Select(async x =>
            {
                var (isq, gpa) = x;
                var childText = isq.Children.Select(y =>
                    y.Children.Length == 0 ? y.InnerHtml.Trim() : y.Children.First().InnerHtml.Trim()).ToList();
                var gpaText = gpa.Children.Select(y =>
                    y.Children.Length == 0 ? y.InnerHtml.Trim() : y.Children.First().InnerHtml.Trim()).ToList();
                var term = new Term(childText[0]);
                var courseCandidates = QueryRepository.Ranged(from course in _context.CourseCodes
                    where course.CourseCode == childText[2]
                    select course, null, term).ToList();

                CourseModel cs;
                if (courseCandidates.Count == 0)
                {
                    _logger.LogWarning(
                        $"No courses found with course code '{childText[2]}' before term '{term}'. Returning null.");
                    cs = null;
                }
                else
                {
                    cs = courseCandidates.Aggregate((a, c) =>
                    {
                        Term? aterm = a.Season == null || a.Year == null
                            ? null
                            : new Term((Season) a.Season, (int) a.Year);
                        Term? cterm = c.Season == null || c.Year == null
                            ? null
                            : new Term((Season) c.Season, (int) c.Year);
                        return aterm >= cterm ? a : c;
                    }).Course;
                }


                return new ISQEntryModel
                {
                    Season = term.Season,
                    Year = term.Year,
                    Course = cs,
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