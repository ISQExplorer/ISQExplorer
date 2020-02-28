using System.Linq;

namespace ISQExplorer.Models
{
    public static class TimedExtensions
    {
        public static IQueryable<T> When<T>(this IQueryable<T> input, TermModel? since, TermModel? until) where T : ITimedModel
        {
            if (since != null && until != null)
            {
                return from x in input
                    where x.Term.Id >= since.Id &&
                          x.Term.Id <= until.Id
                    select x;
            }

            if (since != null)
            {
                 return from x in input
                     where x.Term.Id >= since.Id
                     select x;               
            }

            if (until != null)
            {
                 return from x in input
                     where x.Term.Id <= until.Id
                     select x;               
            }

            return input;
        }
    }
    
    public interface ITimedModel
    {
        TermModel Term { get; }
    }
}