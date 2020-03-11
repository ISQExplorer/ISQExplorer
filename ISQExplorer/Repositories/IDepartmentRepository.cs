using System.Collections.Generic;
using System.Threading.Tasks;
using ISQExplorer.Functional;
using ISQExplorer.Models;

namespace ISQExplorer.Repositories
{
    public interface IDepartmentRepository : IEnumerable<DepartmentModel>
    {
        Task<Optional<DepartmentModel>> FromIdAsync(int id);
        Task<Optional<DepartmentModel>> FromNameAsync(string name);
        Task AddAsync(DepartmentModel department);
        Task AddRangeAsync(IEnumerable<DepartmentModel> departments);
        IEnumerable<DepartmentModel> Departments { get; }
    }
}