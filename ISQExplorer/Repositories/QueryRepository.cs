#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using ISQExplorer.Misc;
using ISQExplorer.Models;
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

        public async Task<IQueryable<ISQEntryModel>> QueryClass(string parameter, QueryType qt, Term? since = null,
            Term? until = null)
        {
            switch (qt)
            {
                case QueryType.CourseCode:
                    return _context.IsqEntries.Where(x => x.Course.CourseCode.Contains(parameter.ToUpper())).When(since, until);
                case QueryType.CourseName:
                    return _context.IsqEntries.Where(x => x.Course.Name.ToUpper().Contains(parameter.ToUpper()))
                        .When(since, until);
                case QueryType.ProfessorName when parameter.Contains(" "):
                {
                    var fname = parameter.Split(" ").SkipLast(1).Join(" ");
                    var lname = parameter.Split(" ").Last();
                    return _context.IsqEntries.Where(x =>
                        x.Professor.FirstName.Contains(fname) && x.Professor.LastName.Contains(lname));
                }
                case QueryType.ProfessorName:
                    return _context.IsqEntries.Where(x => x.Professor.LastName.Contains(parameter)).When(since, until);
                default:
                    throw new ArgumentException($"Invalid QueryType '{qt}'");
            }
        }

        public async Task<IQueryable<ProfessorModel>> NameToProfessors(string professorName)
        {
            if (!professorName.Contains(" "))
            {
                var lname = professorName.ToUpper();

                return from prof in _context.Professors
                    where prof.LastName.ToUpper().Equals(lname)
                    select prof;
            }
            else
            {
                var fname = professorName.Split(" ").SkipLast(1).Join(" ").ToUpper();
                var lname = professorName.Split(" ").Last().ToUpper();

                return from prof in _context.Professors
                    where prof.LastName.ToUpper().Equals(lname) &&
                          prof.FirstName.ToUpper().Equals(fname)
                    select prof;
            }
        }
    }
}