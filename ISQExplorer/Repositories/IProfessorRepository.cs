using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using ISQExplorer.Functional;
using ISQExplorer.Models;

namespace ISQExplorer.Repositories
{
    public interface IProfessorRepository : IEnumerable<ProfessorModel>
    {
        Task<Optional<ProfessorModel>> FromLastName(DepartmentModel dept, string lastName);
        Task<Optional<ProfessorModel>> FromFirstName(DepartmentModel dept, string firstName);
        Task<Optional<ProfessorModel>> FromName(DepartmentModel dept, string name);
        Task<Optional<ProfessorModel>> FromNNumber(DepartmentModel dept, string nNumber);
        Task AddAsync(ProfessorModel prof);
        Task AddRangeAsync(IEnumerable<ProfessorModel> profs);
        IEnumerable<ProfessorModel> Professors { get; }
    }
}