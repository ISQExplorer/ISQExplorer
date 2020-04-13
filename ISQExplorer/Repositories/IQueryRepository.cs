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

    public class Suggestion : IEquatable<Suggestion>
    {
        public readonly QueryType Type;
        public readonly string Value;

        public Suggestion(QueryType qt, string parameter)
        {
            (Type, Value) = (qt, parameter);
        }

        public bool Equals(Suggestion? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Type == other.Type && Value == other.Value;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Suggestion) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int) Type, Value);
        }

        public static bool operator ==(Suggestion? left, Suggestion? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Suggestion? left, Suggestion? right)
        {
            return !Equals(left, right);
        }
    }

    public interface IQueryRepository
    {
        Task<IEnumerable<Suggestion>> QuerySuggestionsAsync(string parameter, QueryType types);

        Task<IQueryable<ISQEntryModel>> QueryEntriesAsync(string parameter, QueryType qt, TermModel? since = null,
            TermModel? until = null);
    }
}