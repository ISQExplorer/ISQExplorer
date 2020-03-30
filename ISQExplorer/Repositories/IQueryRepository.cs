#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ISQExplorer.Models;

namespace ISQExplorer.Repositories
{
    [Flags]
    public enum QueryType
    {
        CourseCode = 1 << 0,
        CourseName = 1 << 1,
        ProfessorName = 1 << 2
    }

    public class Suggestion
    {
        public readonly QueryType Type;
        public readonly string Value;

        public Suggestion(QueryType qt, string parameter)
        {
            (Type, Value) = (qt, parameter);
        }
    }

    public interface IQueryRepository
    {
        Task<IEnumerable<Suggestion>> QuerySuggestionsAsync(string parameter, QueryType types);

        Task<IQueryable<ISQEntryModel>> QueryEntriesAsync(string parameter, QueryType qt, TermModel? since = null,
            TermModel? until = null);
    }
}