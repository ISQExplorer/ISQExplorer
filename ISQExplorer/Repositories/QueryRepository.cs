#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ISQExplorer.Misc;
using ISQExplorer.Models;
using Microsoft.EntityFrameworkCore;
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

        private async Task<IQueryable<ISQEntryModel>> QueryClassSqlLookup(QueryParams qp)
        {
            IQueryable<ISQEntryModel> query = _context.IsqEntries
                .Include(x => x.Course)
                .Include(x => x.Professor)
                .Include(x => x.Professor.Department);

            ISet<string> courseCodes = null;
            if (qp.CourseName != null)
            {
                courseCodes = await Task.Run(() => (from course in _context.Courses
                    where course.Name.ToUpper().Contains(qp.CourseName.ToUpper())
                    select course.CourseCode).ToHashSet());
            }
            else if (qp.CourseCode != null)
            {
                courseCodes = new HashSet<string> {qp.CourseCode};
            }

            if (courseCodes != null)
            {
                query = from entry in query
                    join course in _context.Courses
                        on entry.Course equals course
                    where courseCodes.Contains(course.CourseCode)
                    select entry;
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

            return query.When(qp.Since, qp.Until);
        }

        private async Task<IQueryable<ISQEntryModel>> QueryClassWebLookup(QueryParams qp)
        {
            throw new NotImplementedException();
        }

        public async Task<IQueryable<ISQEntryModel>> QueryClass(QueryParams qp)
        {
            return await QueryClassSqlLookup(qp);
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