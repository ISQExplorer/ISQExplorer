#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AngleSharp;
using ISQExplorer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;

namespace ISQExplorer.Repositories
{
    public class QueryRepository : IQueryRepository
    {
        private ISQExplorerContext _context;
        private ILogger<QueryRepository> _logger;

        public QueryRepository(ISQExplorerContext context, ILogger<QueryRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        private bool QueryIsCached(string? courseCode = null, string? courseName = null,
            string professorName = null, Term? since = null, Term? until = null)
        {
            var (sinceSeason, sinceYear) = Term.ToTuple(since);
            var (untilSeason, untilYear) = Term.ToTuple(until);
            return (from query in _context.Queries
                where query.CourseCode == courseCode?.ToUpper() &&
                      query.CourseName == courseName &&
                      query.SeasonSince == sinceSeason &&
                      query.SeasonUntil == untilSeason &&
                      query.YearSince == sinceYear &&
                      query.YearUntil == untilYear &&
                      DateTime.UtcNow.AddHours(-24) > query.LastUpdated
                select query).Any();
        }

        private async Task<IEnumerable<ISQEntryModel>> QueryWebScrape(string? courseCode = null,
            string? courseName = null,
            string professorName = null, Term? since = null, Term? until = null)
        {
            IEnumerable<string> courseCodes = new List<string> {courseCode};

            if (courseName != null)
            {
                if (courseCode != null)
                {
                    _logger.LogWarning(
                        "Do not pass both courseCode and courseName to QueryWebScrape. Currently the courseName takes precedence.");
                }

                courseCodes = QueryCourseCode(courseName);
            }

            var tasks = new List<Task<ISQEntryModel[]>>();

            foreach (var code in courseCodes)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var url =
                        $"https://banner.unf.edu/pls/nfpo/wksfwbs.p_course_isq_grade?pv_course_id={code.ToUpper()}";
                    var config = Configuration.Default;
                    var context = BrowsingContext.New(config);

                    var document = await context.OpenAsync(req => req.Address(new Url(url)));
                    var tables = document.QuerySelectorAll("table.datadisplaytable > tbody").ToList();
                    var isqTable = tables[3];
                    var gpaTable = tables[6];

                    return await Task.WhenAll(isqTable.Children.Skip(2).Zip(gpaTable.Children.Skip(2)).Select(async x =>
                    {
                        var (isq, gpa) = x;
                        var childText = isq.Children.Select(y => y.InnerHtml).ToList();
                        var gpaText = gpa.Children.Select(y => y.InnerHtml).ToList();
                        var term = new Term(childText[0]);
                        return new ISQEntryModel
                        {
                            Season = term.Season,
                            Year = term.Year,
                            Crn = int.Parse(childText[1]),
                            Professor = (await QueryProfessor(childText[2])).First(),
                            NEnrolled = int.Parse(childText[3]),
                            NTotal = int.Parse(childText[4]),
                            Pct5 = double.Parse(childText[5]),
                            Pct4 = double.Parse(childText[6]),
                            Pct3 = double.Parse(childText[7]),
                            Pct2 = double.Parse(childText[8]),
                            Pct1 = double.Parse(childText[9]),
                            PctNa = double.Parse(childText[10]),
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
                }));
            }

            return (await Task.WhenAll(tasks)).SelectMany(x => x);
        }

        private IEnumerable<ISQEntryModel> QuerySqlLookup(string? courseCode = null,
            string? courseName = null,
            string professorName = null, Term? since = null, Term? until = null)
        {
            IQueryable<ISQEntryModel> query = _context.IsqEntries;

            IQueryable<(ISQEntryModel, T)> Ranged<T>(IQueryable<(ISQEntryModel, T)> input) where T : IRangedModel
            {
                if (since != null && until != null)
                {
                    return from t in input
                        where since.CompareSql(t.Item2.Season, t.Item2.Year) <= 0 &&
                              until.CompareSql(t.Item2.Season, t.Item2.Year) >= 0
                        select t;
                }

                if (since != null)
                {
                    return from t in input
                        where since.CompareSql(t.Item2.Season, t.Item2.Year) <= 0
                        select t;
                }

                if (until != null)
                {
                    return from t in input
                        where until.CompareSql(t.Item2.Season, t.Item2.Year) >= 0
                        select t;
                }

                return input;
            }

            if (courseCode != null)
            {
                query = Ranged(from entry in query
                    join course in _context.Courses
                        on entry.Course equals course
                    join code in _context.CourseCodes
                        on course equals code.Course
                    where code.CourseCode == courseCode.ToUpper()
                    select (entry, code)).Select(x => x.Item1);
            }

            if (professorName != null)
            {
                var temp = from entry in query
                    join professor in _context.Professors
                        on entry.Professor equals professor
                    select new {entry, professor};

                if (professorName.Contains(" "))
                {
                    var name = professorName.Split(" ");
                    var fname = string.Join(" ", name.SkipLast(1));
                    var lname = name.Last();

                    query = from t in temp
                        where t.professor.FirstName == fname && t.professor.LastName == lname
                        select t.entry;
                }
                else
                {
                    query = from t in temp
                        where t.professor.FirstName == professorName || t.professor.LastName == professorName
                        select t.entry;
                }
            }

            return Ranged(from x in query select (x, x)).Select(x => x.Item1);
        }

        private IEnumerable<string> QueryCourseCode(string courseName)
        {
            return from code in _context.CourseCodes
                join name in _context.CourseNames on code.Course equals name.Course
                where name.Name.Contains(courseName)
                select code.CourseCode;
        }

        public async Task<IEnumerable<ProfessorModel>> QueryProfessor(string professorName)
        {
            if (professorName.Contains(" "))
            {
                var name = professorName.Split(" ").ToList();
                var fname = string.Join(" ", name.SkipLast(1));
                var lname = name.Last();

                return from prof in _context.Professors
                    where prof.FirstName == fname &&
                          prof.LastName == lname
                    select prof;
            }
            else
            {
                return from prof in _context.Professors
                    where prof.LastName == professorName
                    select prof;
            }
        }
    }
}