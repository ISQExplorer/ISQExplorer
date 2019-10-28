using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ISQExplorer.Models;
using Microsoft.EntityFrameworkCore;

namespace ISQExplorer.Repositories
{
    public class QueryRepository : IQueryRepository
    {
        private ISQExplorerContext _context;

        public QueryRepository(ISQExplorerContext context)
        {
            _context = context;
        }

        public IEnumerable<ISQEntryModel> Query(string courseCode = null, string className = null,
            string professorName = null, (Term, int)? since = null, (Term, int)? until = null)
        {
            IQueryable<ISQEntryModel> query = _context.IsqEntries;
            if (courseCode != null)
            {
                var temp = from entry in query
                    join course in _context.Courses
                        on entry.Course equals course
                    join code in _context.CourseCodes
                        on course equals code.Course
                    where code.CourseCode == courseCode
                    select new {entry, code};
                if (since != null)
                {
                    query = from t in temp
                        where t.code.SinceTerm != null && t.code.SinceTerm
                        
                }
            }

            if (className != null)
            {
                query = from model in query
                    join name in _context.CourseNames on model equals name.Name
                    select model;
                
                query = query.Join(_context.CourseNames, model => model.CourseCode, name => name.Course,
                    (model, name) => model == name.Course);
            }
            
            return null;
        }
    }
}