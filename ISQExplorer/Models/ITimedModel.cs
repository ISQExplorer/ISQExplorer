using System.Linq;

namespace ISQExplorer.Models
{
    public static class TimedExtensions
    {
        public static IQueryable<T> When<T>(this IQueryable<T> input, Term? since, Term? until) where T : ITimedModel
        {
            if (since != null && until != null)
            {
                return from x in input
                    where x.Year > since.Year || (x.Year == since.Year && x.Season >= since.Season) &&
                          x.Year < until.Year || (x.Year == until.Year && x.Season <= until.Season)
                    select x;
            }

            if (since != null)
            {
                 return from x in input
                     where x.Year > since.Year || (x.Year == since.Year && x.Season >= since.Season)
                     select x;               
            }

            if (until != null)
            {
                 return from x in input
                     where x.Year < until.Year || (x.Year == until.Year && x.Season <= until.Season)
                     select x;               
            }

            return input;
        }
    }
    
    public interface ITimedModel
    {
        Season Season { get; set; }
        int Year { get; set; }
    }
}