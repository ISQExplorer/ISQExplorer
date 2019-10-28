#nullable enable
using System.Collections.Generic;
using ISQExplorer.Models;

namespace ISQExplorer.Repositories
{
    public interface IQueryRepository
    {
        IEnumerable<ISQEntryModel> Query(string? courseCode = null, string? className = null,
            string? professorName = null, (Term, int)? since = null, (Term, int)? until = null);
    }
}