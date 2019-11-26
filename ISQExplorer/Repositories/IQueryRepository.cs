#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using ISQExplorer.Models;

namespace ISQExplorer.Repositories
{
    public class QueryParams
    {
        private string? _courseCode;
        public string? CourseCode
        {
            get => _courseCode;
            set
            {
                if (CourseName != null)
                {
                    throw new ArgumentException($"Do not specify both a course name and a course code. CourseCode = {CourseCode}, CourseName = {CourseName}.");
                }
                _courseCode = value;
            }
        }

        private string? _courseName;

        public string? CourseName
        {
            get => _courseName;
            set
            {
                 if (CourseCode != null)
                 {
                     throw new ArgumentException($"Do not specify both a course name and a course code. CourseCode = {CourseCode}, CourseName = {CourseName}.");
                 }
                 _courseName = value;               
            }
        }
        
        public string? ProfessorName { get; set; }
        public Term? Since { get; set; }
        public Term? Until { get; set; }

        public QueryParams(string? courseCode = null, string? courseName = null, string? professorName = null,
            Term? since = null, Term? until = null)
        {
            (CourseCode, CourseName, ProfessorName, Since, Until) =
                (courseCode, courseName, professorName, since, until);
        }

        public QueryParams(QueryParams other) : this(other.CourseCode, other.CourseName, other.ProfessorName,
            other.Since, other.Until)
        {
        }
    }

    public interface IQueryRepository
    {
        Task<IEnumerable<ISQEntryModel>> QueryClass(QueryParams qp);
        Task<IEnumerable<ProfessorModel>> NameToProfessors(string professorName);
    }
}