using System;

namespace ISQExplorer.Exceptions
{
    public class ProfessorScrapeException : Exception
    {
        public readonly string NNumber;
        public readonly string Reason;

        public ProfessorScrapeException(string nNumber, string reason) : base(
            $"Failed to scrape professor '{nNumber}'. {reason}")
        {
            (NNumber, Reason) = (nNumber, reason);
        }

        public ProfessorScrapeException(string nNumber, string reason, Exception innerException) : base(
            $"Failed to scrape professor '{nNumber}'. {reason}", innerException)
        {
            (NNumber, Reason) = (nNumber, reason);
        }
    }
}