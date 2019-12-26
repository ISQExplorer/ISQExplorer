#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ISQExplorer.Models;

namespace ISQExplorer.Repositories
{
    public class QueryParams : IEquatable<QueryParams>
    {
        private string? _courseCode;

        public string? CourseCode
        {
            get => _courseCode;
            set
            {
                if (CourseName != null)
                {
                    throw new ArgumentException(
                        $"Do not specify both a course name and a course code. CourseCode = {CourseCode}, CourseName = {CourseName}.");
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
                    throw new ArgumentException(
                        $"Do not specify both a course name and a course code. CourseCode = {CourseCode}, CourseName = {CourseName}.");
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

        public bool Equals(QueryParams other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _courseCode == other._courseCode && _courseName == other._courseName &&
                   ProfessorName == other.ProfessorName && Equals(Since, other.Since) && Equals(Until, other.Until);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((QueryParams) obj);
        }

        public static bool operator ==(QueryParams q1, QueryParams q2) =>
            ReferenceEquals(q1, q2) || (!ReferenceEquals(q1, null) && q1.Equals(q2));

        public static bool operator !=(QueryParams q1, QueryParams q2) => !(q1 == q2);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (_courseCode != null ? _courseCode.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (_courseName != null ? _courseName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ProfessorName != null ? ProfessorName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Since != null ? Since.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Until != null ? Until.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public interface IQueryRepository
    {
        Task<IEnumerable<ISQEntryModel>> QueryClass(QueryParams qp);
        Task<IEnumerable<ProfessorModel>> NameToProfessors(string professorName);
    }
}