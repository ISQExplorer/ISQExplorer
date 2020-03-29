#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ISQExplorer.Misc;
using ISQExplorer.Models;
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

        public async IAsyncEnumerable<Suggestion> QuerySuggestionsAsync(string parameter, QueryType queryTypes)
        {
            parameter = parameter.ToLower();

            int SuggestionOrderer(string s1, string s2)
            {
                s1 = s1.ToLower();
                s2 = s2.ToLower();

                if (s1 == s2)
                {
                    return 0;
                }

                var whitespace = new[] {' ', '\t', '\n'};

                foreach (var pair in new[] {(s1, s2)}.Concat(s1.Split(whitespace).Zip(s2.Split(whitespace))))
                {
                    var (word1, word2) = pair;

                    if (word1.StartsWith(parameter))
                    {
                        if (word2.StartsWith(parameter))
                        {
                            return word1.Length - word2.Length;
                        }

                        return -1;
                    }

                    if (word2.StartsWith(parameter))
                    {
                        return 1;
                    }
                }

                return 0;
            }

            yield break;
        }

        public async Task<IQueryable<ISQEntryModel>> QueryEntriesAsync(string parameter, QueryType qt,
            TermModel? since = null,
            TermModel? until = null) => await Task.Run(() =>
        {
            switch (qt)
            {
                case QueryType.CourseCode:
                    return _context.IsqEntries.Where(x => x.Course.CourseCode.Contains(parameter.ToUpper()))
                        .When(since, until);
                case QueryType.CourseName:
                    return _context.IsqEntries.Where(x => x.Course.Name.ToUpper().Contains(parameter.ToUpper()))
                        .When(since, until);
                case QueryType.ProfessorName when parameter.Contains(" "):
                {
                    var fname = parameter.Split(" ").SkipLast(1).Join(" ");
                    var lname = parameter.Split(" ").Last();
                    return _context.IsqEntries.Where(x =>
                        x.Professor.FirstName.ToUpper().Contains(fname.ToUpper()) &&
                        x.Professor.LastName.ToUpper().Contains(lname.ToUpper()));
                }
                case QueryType.ProfessorName:
                    return _context.IsqEntries.Where(x => x.Professor.LastName.ToUpper()
                        .Contains(parameter.ToUpper())).When(since, until);
                default:
                    throw new ArgumentException($"Invalid QueryType '{qt}'. You can only query one type at a time.");
            }
        });
    }
}