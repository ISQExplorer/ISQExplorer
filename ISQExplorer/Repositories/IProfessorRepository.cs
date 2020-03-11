using System.Collections.Generic;
using System.Threading.Tasks;
using ISQExplorer.Functional;
using ISQExplorer.Models;

namespace ISQExplorer.Repositories
{
    public interface IProfessorRepository : IEnumerable<ProfessorModel>
    {
        Task<Optional<ProfessorModel>> FromLastNameAsync(DepartmentModel dept, string lastName);
        Task<Optional<ProfessorModel>> FromFirstNameAsync(DepartmentModel dept, string firstName);
        Task<Optional<ProfessorModel>> FromNameAsync(DepartmentModel dept, string name);
        Task<Optional<ProfessorModel>> FromNNumberAsync(DepartmentModel dept, string nNumber);
        Task AddAsync(ProfessorModel prof);
        Task AddRangeAsync(IEnumerable<ProfessorModel> profs);
        IEnumerable<ProfessorModel> Professors { get; }
    }
}