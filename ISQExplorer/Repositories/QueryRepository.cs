#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
                int res;

                bool CompareInt(int a, int b)
                {
                    if (a == b)
                    {
                        res = 0;
                        return false;
                    }

                    res = a - b;
                    return true;
                }

                bool CompareBool(bool a, bool b)
                {
                    if (a == b)
                    {
                        res = 0;
                        return false;
                    }

                    if (a)
                    {
                        res = -1;
                        return true;
                    }

                    res = 1;
                    return true;
                }

                s1 = s1.ToLower().Trim().Split(whitespace).Join(" ");
                s2 = s2.ToLower().Trim().Split(whitespace).Join(" ");

                var s1Lcs = Algorithms.LongestCommonSubstring(s1, parameter)
                    .OrderBy(x => x.Substring.Length)
                    .ThenBy(x => x.Index)
                    .ToList();
                var s2Lcs = Algorithms.LongestCommonSubstring(s2, parameter)
                    .OrderBy(x => x.Substring.Length)
                    .ThenBy(x => x.Index)
                    .ToList();

                if (s1Lcs.None() && s2Lcs.None())
                {
                    return 0;
                }

                if (CompareBool(s1Lcs.None(), s2Lcs.None()))
                {
                    return res;
                }

                if (CompareInt(s1Lcs.Select(x => x.Substring.Length).Max(),
                    s2Lcs.Select(x => x.Substring.Length).Max()))
                {
                    return res;
                }

                var s1Spos = new[] {0}.Concat(s1.IndexOfAll(@"\s+").Select(x => x + 1)).ToList();
                var s2Spos = new[] {0}.Concat(s2.IndexOfAll(@"\s+").Select(x => x + 1)).ToList();

                var s1MatchRelInfo = s1Lcs
                    .Select(x => (x.Index, x.Substring, LastWord: s1Spos.LastOr(-1, y => y <= x.Index)))
                    .Where(x => x.LastWord >= 0)
                    .ToList();

                var s1MatchRelPos = s1MatchRelInfo
                    .Select(x => x.Index - x.LastWord)
                    .ToList();

                var s2MatchRelInfo = s2Lcs
                    .Select(x => (x.Index, x.Substring, LastWord: s2Spos.LastOr(-1, y => y <= x.Index)))
                    .Where(x => x.LastWord >= 0)
                    .ToList();

                var s2MatchRelPos = s2MatchRelInfo
                    .Select(x => x.Index - x.LastWord)
                    .ToList();

                if (CompareInt(s1MatchRelPos.Min(), s2MatchRelPos.Min()))
                {
                    return res;
                }

                if (CompareInt(s1MatchRelPos.IndexOf(s1MatchRelPos.Min()), s2MatchRelPos.IndexOf(s2MatchRelPos.Min())))
                {
                    return res;
                }

                var s1CharsUnfilled = s1.Split(" ")
                    .Select(x => x.Length)
                    .Zip(s1MatchRelInfo)
                    .Select(x => (CharsMissing: x.First - x.Second.Substring.Length, x.Second.Index))
                    .ToList();
                var s2CharsUnfilled = s2.Split(" ")
                    .Select(x => x.Length)
                    .Zip(s2MatchRelInfo)
                    .Select(x => (CharsMissing: x.First - x.Second.Substring.Length, x.Second.Index))
                    .ToList();

                if (CompareInt(s1CharsUnfilled.Min(x => x.CharsMissing), s2CharsUnfilled.Min(x => x.CharsMissing)))
                {
                    return res;
                }

                if (CompareInt(s1CharsUnfilled.Min(x => x.Index), s2CharsUnfilled.Min(x => x.Index)))
                {
                    return res;
                }

                if (CompareInt(s1Lcs.Count, s2Lcs.Count))
                {
                    return res;
                }

                if (CompareInt(s1Lcs.First().Index, s2Lcs.First().Index))
                {
                    return res;
                }

                return string.Compare(s1, s2, StringComparison.Ordinal);
            }

            IEnumerable<Suggestion> res = new List<Suggestion>();
            if ((queryTypes & QueryType.CourseCode) > 0)
            {
                res = res.Concat(_context.Courses
                    .Where(c => c.CourseCode.ToLower().Contains(parameter))
                    .Take(100)
                    .AsEnumerable()
                    .Select(x => new Suggestion(QueryType.CourseCode, x.CourseCode, x.Name)));
            }

            if ((queryTypes & QueryType.CourseName) > 0)
            {
                res = res.Concat(_context.Courses
                    .Where(c => c.Name.ToLower().Contains(parameter))
                    .Take(100)
                    .AsEnumerable()
                    .Select(x => new Suggestion(QueryType.CourseName, x.Name, x.CourseCode)));
            }

            if ((queryTypes & QueryType.ProfessorName) > 0)
            {
                res = res.Concat(_context.Professors
                    .Where(c => (c.FirstName + " " + c.LastName).ToLower().Contains(parameter))
                    .Take(100)
                    .AsEnumerable()
                    .Select(x => new Suggestion(QueryType.ProfessorName, x.FirstName + " " + x.LastName, x.NNumber)));
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
                        .Where(x => x.Course.CourseCode == parameter.ToUpper())
                        .When(since, until);
                case QueryType.CourseName:
                    return _context.IsqEntries
                        .Include(x => x.Course)
                        .Include(x => x.Course.Department)
                        .Include(x => x.Professor)
                        .Include(x => x.Professor.Department)
                        .Include(x => x.Term)
                        .Where(x => x.Course.Name.ToUpper() == parameter.ToUpper())
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
                            x.Professor.FirstName.ToUpper() == fname.ToUpper() &&
                            x.Professor.LastName.ToUpper() == lname.ToUpper());
                }
                case QueryType.ProfessorName:
                    return _context.IsqEntries
                        .Include(x => x.Course)
                        .Include(x => x.Course.Department)
                        .Include(x => x.Professor)
                        .Include(x => x.Professor.Department)
                        .Include(x => x.Term)
                        .Where(x => x.Professor.LastName.ToUpper() == parameter.ToUpper()).When(since, until);
                default:
                    throw new ArgumentException($"Invalid QueryType '{qt}'. You can only query one type at a time.");
            }
        });
    }
}