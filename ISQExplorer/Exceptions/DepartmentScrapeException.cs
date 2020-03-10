using System;
using ISQExplorer.Models;

namespace ISQExplorer.Exceptions
{
    public class DepartmentScrapeException : Exception
    {
        public readonly DepartmentModel Department;
        public readonly string Reason;

        public DepartmentScrapeException(DepartmentModel dept, string reason) : base(
            $"Failed to scrape department '{dept}'s listings. {reason}")
        {
            (Department, Reason) = (dept, reason);
        }

        public DepartmentScrapeException(DepartmentModel dept, string reason, Exception innerException) : base(
            $"Failed to scrape department '{dept}'s listings. {reason}", innerException)
        {
            (Department, Reason) = (dept, reason);
        }
    }
}