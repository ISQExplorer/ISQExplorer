using System;
using ISQExplorer.Models;

namespace ISQExplorer.Exceptions
{
    public class CourseScrapeException : Exception
    {
        public readonly string Reason;
        public readonly string? CourseCode;
        public readonly DepartmentModel? Department;
        public readonly TermModel? Term;

        public CourseScrapeException(string reason, string? courseCode = null, DepartmentModel? dept = null,
            TermModel? term = null) : base(
            $"Failed to scrape course '{courseCode}'. {reason}.{(dept != null ? " Dept:" + dept : "")}{(term != null ? " Term:" + term : "")}")
        {
            (CourseCode, Reason, Department, Term) = (courseCode, reason, dept, term);
        }

        public CourseScrapeException(string reason, Exception innerException, string? courseCode = null,
            DepartmentModel? dept = null,
            TermModel? term = null) : base(
            $"Failed to scrape course '{courseCode}'. {reason}.{(dept != null ? " Dept:" + dept : "")}{(term != null ? " Term:" + term : "")}",
            innerException)
        {
            (CourseCode, Reason, Department, Term) = (courseCode, reason, dept, term);
        }
    }
}