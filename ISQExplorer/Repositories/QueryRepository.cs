#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Common;
using AngleSharp.Text;
using ISQExplorer.Misc;
using ISQExplorer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;

namespace ISQExplorer.Repositories
{
    public class QueryRepository : IQueryRepository
    {
        private readonly ISQExplorerContext _context;
        private readonly ILogger<QueryRepository> _logger;

        public QueryRepository(ISQExplorerContext context, ILogger<QueryRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public Task<IEnumerable<Suggestion>> QuerySuggestionsAsync(string parameter, QueryType queryTypes)
        {
            var whitespace = new[] {' ', '\t', '\n'};

            parameter = parameter.ToLower().Split(whitespace).Join(" ");

            int SuggestionOrderer(string s1, string s2)
            {
                s1 = s1.ToLower().Split(whitespace).Join(" ");
                s2 = s2.ToLower().Split(whitespace).Join(" ");

                var s1Lcs = Algorithms.LongestCommonSubstring(parameter, s1)
                    .OrderBy(x => x.Substring.Length)
                    .ThenBy(x => x.Index)
                    .ToList();
                var s2Lcs = Algorithms.LongestCommonSubstring(parameter, s2)
                    .OrderBy(x => x.Substring.Length)
                    .ThenBy(x => x.Index)
                    .ToList();

                if (s1Lcs.None() && s2Lcs.None())
                {
                    return 0;
                }

                if (s1Lcs.None())
                {
                    return 1;
                }

                if (s2Lcs.None())
                {
                    return -1;
                }

                if (s1Lcs.First().Substring.Length < s2Lcs.First().Substring.Length)
                {
                    return -1;
                }

                if (s1Lcs.First().Substring.Length > s2Lcs.First().Substring.Length)
                {
                    return 1;
                }

                if (s1Lcs.First().Index < s2Lcs.First().Index)
                {
                    return -1;
                }

                if (s1Lcs.First().Index > s2Lcs.First().Index)
                {
                    return 1;
                }

                return s1Lcs.Count - s2Lcs.Count;
            }

            IEnumerable<Suggestion> res = new List<Suggestion>();
            if ((queryTypes & QueryType.CourseCode) > 0)
            {
                res = res.Concat(_context.Courses
                    .Where(c => c.CourseCode.Contains(parameter))
                    .Take(100)
                    .AsEnumerable()
                    .Select(x => new Suggestion(QueryType.CourseCode, x.CourseCode)));
            }

            if ((queryTypes & QueryType.CourseName) > 0)
            {
                res = res.Concat(_context.Courses
                    .Where(c => c.Name.Contains(parameter))
                    .Take(100)
                    .AsEnumerable()
                    .Select(x => new Suggestion(QueryType.CourseName, x.Name)));
            }

            if ((queryTypes & QueryType.ProfessorName) > 0)
            {
                res = res.Concat(_context.Professors
                    .Where(c => (c.FirstName + " " + c.LastName).Contains(parameter))
                    .Take(100)
                    .AsEnumerable()
                    .Select(x => new Suggestion(QueryType.CourseName, x.FirstName + " " + x.LastName)));
            }

            return Task.FromResult(res.OrderBy((s1, s2) => SuggestionOrderer(s1.Value, s2.Value)).AsEnumerable());
        }

        public async Task<IQueryable<ISQEntryModel>> QueryEntriesAsync(string parameter, QueryType qt,
            TermModel? since = null,
            TermModel? until = null) => await Task.Run(() =>
        {
            switch (qt)
            {
                case QueryType.CourseCode:
                    return _context.IsqEntries
                        .Include(x => x.Course)
                        .Include(x => x.Course.Department)
                        .Include(x => x.Professor)
                        .Include(x => x.Professor.Department)
                        .Include(x => x.Term)
                        .Where(x => x.Course.CourseCode.Contains(parameter.ToUpper()))
                        .When(since, until);
                case QueryType.CourseName:
                    return _context.IsqEntries
                        .Include(x => x.Course)
                        .Include(x => x.Course.Department)
                        .Include(x => x.Professor)
                        .Include(x => x.Professor.Department)
                        .Include(x => x.Term)
                        .Where(x => x.Course.Name.ToUpper().Contains(parameter.ToUpper()))
                        .When(since, until);
                case QueryType.ProfessorName when parameter.Contains(" "):
                {
                    var fname = parameter.Split(" ").SkipLast(1).Join(" ");
                    var lname = parameter.Split(" ").Last();
                    return _context.IsqEntries
                        .Include(x => x.Course)
                        .Include(x => x.Course.Department)
                        .Include(x => x.Professor)
                        .Include(x => x.Professor.Department)
                        .Include(x => x.Term)
                        .Where(x =>
                            x.Professor.FirstName.ToUpper().Contains(fname.ToUpper()) &&
                            x.Professor.LastName.ToUpper().Contains(lname.ToUpper()));
                }
                case QueryType.ProfessorName:
                    return _context.IsqEntries
                        .Include(x => x.Course)
                        .Include(x => x.Course.Department)
                        .Include(x => x.Professor)
                        .Include(x => x.Professor.Department)
                        .Include(x => x.Term)
                        .Where(x => x.Professor.LastName.ToUpper()
                            .Contains(parameter.ToUpper())).When(since, until);
                default:
                    throw new ArgumentException($"Invalid QueryType '{qt}'. You can only query one type at a time.");
            }
        });
    }
}