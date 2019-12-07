#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AngleSharp;
using ISQExplorer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ISQExplorer.Repositories
{
    public class QueryHelper
    {
        private readonly ISQExplorerContext _context;
        private readonly ILogger<QueryRepository> _logger;

        public QueryHelper(ISQExplorerContext context, ILogger<QueryRepository> logger)
        {
            (_context, _logger) = (context, logger);
        }

        public async Task<bool> QueryIsCached(QueryParams p)
        {
            var (sinceSeason, sinceYear) = p.Since;
            var (untilSeason, untilYear) = p.Until;
            return await (from query in _context.Queries
                where (query.CourseCode == null ||
                       p.CourseCode != null && query.CourseCode == p.CourseCode.ToUpper()) &&
                      (query.CourseName == p.CourseName || query.CourseName == null) &&
                      (query.ProfessorName == p.ProfessorName || query.ProfessorName == null) &&
                      (query.SeasonSince == sinceSeason || query.SeasonSince == null) &&
                      (query.SeasonUntil == untilSeason || query.SeasonUntil == null) &&
                      (query.YearSince == sinceYear || query.YearSince == null) &&
                      (query.YearUntil == untilYear || query.YearUntil == null) &&
                      query.LastUpdated > DateTime.UtcNow.AddHours(-24)
                select query).AnyAsync();
        }

        public async Task<IEnumerable<string>> CourseNameToCourseCodes(string courseName)
        {
            return await Task.Run(() => from code in _context.CourseCodes
                join name in _context.CourseNames on code.Course equals name.Course
                where name.Name.ToLower().Contains(courseName.ToLower())
                select code.CourseCode);
        }

        public async Task<CourseModel> CourseCodeToCourse(string courseCode, Term? since = null, Term? until = null)
        {
            courseCode = courseCode.ToUpper();

            var courses = QueryRepository.Ranged(await Task.Run(() => from code in _context.CourseCodes
                where courseCode == code.CourseCode
                select code), since, until).Select(x => x.Course).ToList();

            if (courses.Count == 0)
            {
                var msg =
                    $"No course found for course code '{courseCode}' within the timeframe [{since?.ToString() ?? "any"}, {until?.ToString() ?? "any"}]";
                _logger.LogWarning(msg);
                throw new ArgumentException(msg, nameof(courseCode));
            }

            if (courses.Count > 1)
            {
                _logger.LogWarning(
                    $"More than one course found for '{courseCode}' within the timeframe [{since?.ToString() ?? "any"}, {until?.ToString() ?? "any"}]. Returning a random one.");
            }

            return courses.First();
        }

        public async Task<IEnumerable<ProfessorModel>> NameToProfessors(string professorName)
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

        public async Task<IEnumerable<ISQEntryModel>> WebScrapeCourseCode(string courseCode)
        {
            CourseModel course;
            try
            {
                course = await CourseCodeToCourse(courseCode);
            }
            catch (ArgumentException)
            {
                _logger.LogWarning($"No course found in database with course code '{courseCode}'.");
                return new ISQEntryModel[0];
            }

            var url =
                $"https://banner.unf.edu/pls/nfpo/wksfwbs.p_course_isq_grade?pv_course_id={courseCode.ToUpper()}";
            var config = Configuration.Default;
            var browsingContext = BrowsingContext.New(config);
            using var webClient = new WebClient();
            var webContent = await Task.Run(() => webClient.DownloadString(url));

            var document = await browsingContext.OpenAsync(req => req.Content(webContent));
            var tables = document.QuerySelectorAll("table.datadisplaytable > tbody").ToList();
            if (tables.Count < 6)
            {
                _logger.LogWarning(
                    $"Malformed page at url '{url}'. Most likely the given course code '{courseCode}' is invalid. Returning a blank list.");
                return new ISQEntryModel[0];
            }

            var isqTable = tables[3];
            var gpaTable = tables[5];

            var professorCache = new ConcurrentDictionary<string, ProfessorModel>();

            async Task<ProfessorModel?> GetProfessor(string name)
            {
                if (!professorCache.ContainsKey(name))
                {
                    var professors = (await NameToProfessors(name)).ToList();
                    if (professors.Count == 0)
                    {
                        string msg = $"No professors found for name '{name}'. Returning null for this entry.";
                        _logger.LogWarning(msg);
                        return null;
                    }

                    if (professors.Count > 1)
                    {
                        _logger.LogWarning(
                            $"More than one professor found for name '${name}' with first names [{string.Join(", ", professors.Select(x => x.FirstName))}]. Using the first one in the list.");
                    }

                    professorCache[name] = professors.First();
                }

                return professorCache[name];
            }

            return (await Task.WhenAll(isqTable.Children.Skip(2).Zip(gpaTable.Children.Skip(2)).Select(async x =>
            {
                var (isq, gpa) = x;
                var childText = isq.Children.Select(y =>
                    y.Children.Length == 0 ? y.InnerHtml.Trim() : y.Children.First().InnerHtml.Trim()).ToList();
                var gpaText = gpa.Children.Select(y =>
                    y.Children.Length == 0 ? y.InnerHtml.Trim() : y.Children.First().InnerHtml.Trim()).ToList();
                var term = new Term(childText[0]);
                var professor = await GetProfessor(childText[2]);

                return new ISQEntryModel
                {
                    Course = course,
                    Season = term.Season,
                    Year = term.Year,
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

        public async Task<IEnumerable<ISQEntryModel>> WebScrapeNNumber(string nNumber)
        {
            var url =
                $"https://banner.unf.edu/pls/nfpo/wksfwbs.p_instructor_isq_grade?pv_instructor={nNumber.ToUpper()}";
            var config = Configuration.Default;
            var context = BrowsingContext.New(config);

            using var webClient = new WebClient();
            var webContent = await Task.Run(() => webClient.DownloadString(url));
            var document = await context.OpenAsync(req => req.Content(webContent));

            var tables = document.QuerySelectorAll("table.datadisplaytable > tbody").ToList();
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

    public class QueryRepository : IQueryRepository
    {
        private readonly ISQExplorerContext _context;
        private readonly ILogger<QueryRepository> _logger;
        private readonly QueryHelper _helper;

        public QueryRepository(ISQExplorerContext context, ILogger<QueryRepository> logger)
        {
            _context = context;
            _logger = logger;
            _helper = new QueryHelper(context, logger);
        }

        public static IQueryable<T> Ranged<T>(IQueryable<T> input, Term? since,
            Term? until)
            where T : IRangedModel
        {
            if (since != null && until != null)
            {
                return from t in input
                    where (since.Year < t.Year || (since.Year == t.Year && since.Season <= t.Season)) &&
                          (t.Year == null && until.Year > t.Year || (until.Year == t.Year && until.Season >= t.Season))
                    select t;
            }

            if (since != null)
            {
                return from t in input
                    where (since.Year < t.Year || (since.Year == t.Year && since.Season <= t.Season))
                    select t;
            }

            if (until != null)
            {
                return from t in input
                    where (t.Year == null || until.Year > t.Year || (until.Year == t.Year && until.Season >= t.Season))
                    select t;
            }

            return input;
        }

        public static IQueryable<ISQEntryModel> Ranged(IQueryable<ISQEntryModel> input, Term? since,
            Term? until)
        {
            if (since != null && until != null)
            {
                return from t in input
                    where (since.Year < t.Year || (since.Year == t.Year && since.Season <= t.Season)) &&
                          (until.Year > t.Year || (until.Year == t.Year && until.Season >= t.Season))
                    select t;
            }

            if (since != null)
            {
                return from t in input
                    where (since.Year < t.Year || (since.Year == t.Year && since.Season <= t.Season))
                    select t;
            }

            if (until != null)
            {
                return from t in input
                    where (until.Year > t.Year || (until.Year == t.Year && until.Season >= t.Season))
                    select t;
            }

            return input;
        }

        private IEnumerable<ISQEntryModel> QueryClassSqlLookup(QueryParams qp)
        {
            IQueryable<ISQEntryModel> query = _context.IsqEntries;

            IEnumerable<string> courseCodes = null;
            if (qp.CourseName != null)
            {
                courseCodes = (from name in _context.CourseNames
                    join code in _context.CourseCodes on name.Course equals code.Course
                    where name.Name.ToUpper().Contains(qp.CourseName.ToUpper())
                    select name.Name).ToHashSet();
            }
            else if (qp.CourseCode != null)
            {
                courseCodes = new List<string> {qp.CourseCode};
            }

            if (courseCodes != null)
            {
                query = Ranged(from entry in query
                    join course in _context.Courses
                        on entry.Course equals course
                    join code in _context.CourseCodes
                        on course equals code.Course
                    where courseCodes.Contains(code.CourseCode)
                    select entry, qp.Since, qp.Until);
            }

            if (qp.ProfessorName != null)
            {
                var temp = from entry in query
                    join professor in _context.Professors
                        on entry.Professor equals professor
                    select new {entry, professor};

                if (qp.ProfessorName.Contains(" "))
                {
                    var name = qp.ProfessorName.Split(" ");
                    var fname = string.Join(" ", name.SkipLast(1));
                    var lname = name.Last();

                    query = from t in temp
                        where t.professor.FirstName == fname && t.professor.LastName == lname
                        select t.entry;
                }
                else
                {
                    query = from t in temp
                        where t.professor.FirstName == qp.ProfessorName || t.professor.LastName == qp.ProfessorName
                        select t.entry;
                }
            }

            return Ranged(query, qp.Since, qp.Until);
        }

        private async Task<IEnumerable<ISQEntryModel>> QueryClassWebLookup(QueryParams qp)
        {
            Task<IEnumerable<ISQEntryModel>>? courseQuery = null;
            Task<IEnumerable<ISQEntryModel>>? professorQuery = null;

            if (qp.CourseCode != null)
            {
                courseQuery = _helper.WebScrapeCourseCode(qp.CourseCode);
            }
            else if (qp.CourseName != null)
            {
                var courseCodes = await _helper.CourseNameToCourseCodes(qp.CourseName);
                courseQuery = Task.Run(async () =>
                    (await Task.WhenAll(courseCodes.Select(x => _helper.WebScrapeCourseCode(x)))).SelectMany(x => x));
            }

            if (qp.ProfessorName != null)
            {
                var professors = (await _helper.NameToProfessors(qp.ProfessorName)).Select(x => x.NNumber);
                professorQuery = Task.Run(async () =>
                    (await Task.WhenAll(professors.Select(x => _helper.WebScrapeNNumber(x)))).SelectMany(x => x));
            }

            if (courseQuery != null && professorQuery != null)
            {
                return Ranged((await courseQuery).Intersect(await professorQuery).AsQueryable(), qp.Since,
                    qp.Until);
            }
            else if (courseQuery != null)
            {
                return Ranged((await courseQuery).AsQueryable(), qp.Since, qp.Until);
            }
            else if (professorQuery != null)
            {
                return Ranged((await professorQuery).AsQueryable(), qp.Since, qp.Until);
            }

            return new List<ISQEntryModel>().AsQueryable();
        }

        public async Task<IEnumerable<ISQEntryModel>> QueryClass(QueryParams qp)
        {
            // we don't really care when this is done as long as it's done eventually, so don't await
#pragma warning disable 4014
            _context.Queries.Where(x => DateTime.UtcNow.AddHours(-24) > x.LastUpdated)
                .ForEachAsync(x => _context.Remove(x));
#pragma warning restore 4014

            if (await _helper.QueryIsCached(qp))
            {
                return QueryClassSqlLookup(qp);
            }

            var results = (await QueryClassWebLookup(qp)).ToList();
            _context.IsqEntries.AddRange(results);
            _context.Queries.Add(new QueryModel
            {
                CourseCode = qp.CourseCode,
                CourseName = qp.CourseName,
                ProfessorName = qp.ProfessorName,
                SeasonSince = qp.Since?.Season,
                SeasonUntil = qp.Until?.Season,
                YearSince = qp.Since?.Year,
                YearUntil = qp.Until?.Year
            });
            _context.SaveChanges();
            return results;
        }

        public Task<IEnumerable<ProfessorModel>> NameToProfessors(string professorName)
        {
            return _helper.NameToProfessors(professorName);
        }
    }
}