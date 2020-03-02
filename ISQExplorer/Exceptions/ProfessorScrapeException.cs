using System;

namespace ISQExplorer.Exceptions
{
    public class ProfessorScrapeException : Exception
    {
        public readonly string? NNumber;
        public readonly string Reason;

        public ProfessorScrapeException(string reason, string? nNumber = null) : base(
            $"Failed to scrape professor '{nNumber ?? "blank"}'. {reason}")
        {
            (NNumber, Reason) = (nNumber, reason);
        }

        public ProfessorScrapeException(string reason, Exception innerException, string? nNumber = null) : base(
            $"Failed to scrape professor '{nNumber ?? "blank"}'. {reason}", innerException)
        {
            (NNumber, Reason) = (nNumber, reason);
        }
    }
}