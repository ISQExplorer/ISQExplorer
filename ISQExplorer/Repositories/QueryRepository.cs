#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AngleSharp;
using ISQExplorer.Models;
using ISQExplorer.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ISQExplorer.Repositories
{
    public class QueryRepository : IQueryRepository
    {
        private readonly ISQExplorerContext _context;
        private readonly DataScraper _ds;

        public QueryRepository(ISQExplorerContext context, DataScraper ds)
        {
            _context = context;
            _ds = ds;
        }

        private IEnumerable<ISQEntryModel> QueryClassSqlLookup(QueryParams qp)
        {
            IQueryable<ISQEntryModel> query = _context.IsqEntries;

            IEnumerable<string> courseCodes = null;
            if (qp.CourseName != null)
            {
                courseCodes = from course in _context.Courses
                    where course.Name.ToUpper().Contains(qp.CourseName.ToUpper())
                    select course.CourseCode;
            }
            else if (qp.CourseCode != null)
            {
                courseCodes = new List<string> {qp.CourseCode};
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

        private async Task<IEnumerable<ISQEntryModel>> QueryClassWebLookup(QueryParams qp)
        {
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