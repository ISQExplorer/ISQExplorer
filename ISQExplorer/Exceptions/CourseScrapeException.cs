using System;

namespace ISQExplorer.Exceptions
{
    public class CourseScrapeException : Exception
    {
        public readonly string CourseCode;
        public readonly string Reason;
        
        public CourseScrapeException(string courseCode, string reason) : base($"Failed to scrape course '{courseCode}'. {reason}")
        {
            (CourseCode, Reason) = (courseCode, reason);
        }

        public CourseScrapeException(string courseCode, string reason, Exception innerException) : base(
            $"Failed to scrape course '{courseCode}'. {reason}", innerException)
        {
            (CourseCode, Reason) = (courseCode, reason);
        }
    }
}