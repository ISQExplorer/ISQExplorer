using System;
using ISQExplorer.Models;

namespace ISQExplorer.Exceptions
{
    public class ProfessorScrapeException : Exception
    {
        public readonly string? NNumber;
        public readonly string Reason;
        public readonly DepartmentModel? Department;
        public readonly TermModel? Term;

        public ProfessorScrapeException(string reason, string? nNumber = null, DepartmentModel? dept = null,
            TermModel? term = null) : base(
            $"Failed to scrape professor '{nNumber ?? "blank"}'. {reason}.{(dept != null ? " Dept:" + dept : "")}{(term != null ? " Term:" + term : "")}")
        {
            (NNumber, Reason, Department, Term) = (nNumber, reason, dept, term);
        }

        public ProfessorScrapeException(string reason, Exception innerException, string? nNumber = null,
            DepartmentModel? dept = null, TermModel? term = null) : base(
            $"Failed to scrape professor '{nNumber ?? "blank"}'. {reason}.{(dept != null ? " Dept:" + dept : "")}{(term != null ? " Term:" + term : "")}", innerException)
        {
            (NNumber, Reason, Department, Term) = (nNumber, reason, dept, term);
        }
    }
}