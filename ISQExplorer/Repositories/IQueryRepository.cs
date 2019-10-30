#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using ISQExplorer.Models;

namespace ISQExplorer.Repositories
{
    public interface IQueryRepository
    {
        Task<IEnumerable<ISQEntryModel>> Query(string? courseCode = null, string? courseName = null,
            string? professorName = null, Term? since = null, Term? until = null);

        Task<IEnumerable<ProfessorModel>> QueryProfessor(string professorName);
    }
}