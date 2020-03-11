using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ISQExplorer.Functional;
using ISQExplorer.Misc;
using ISQExplorer.Models;

namespace ISQExplorer.Repositories
{
    public class DepartmentRepository : IDepartmentRepository
    {
        private readonly IDictionary<int, DepartmentModel> _idToDepartment;
        private readonly IDictionary<string, DepartmentModel> _nameToDepartment;
        private readonly ISQExplorerContext _context;
        private readonly ReadWriteLock _lock;

        private void _addDepartment(DepartmentModel department)
        {
            _idToDepartment[department.Id] = department;
            _nameToDepartment[department.Name] = department;
        }

        public DepartmentRepository(ISQExplorerContext context)
        {
            _idToDepartment = new Dictionary<int, DepartmentModel>();
            _nameToDepartment = new Dictionary<string, DepartmentModel>();
            _context = context;
            _lock = new ReadWriteLock();
        }

        public Task AddAsync(DepartmentModel department) => _lock.WriteAsync(async () =>
        {
            _addDepartment(department);
            _context.Departments.Add(department);
            await _context.SaveChangesAsync();
        });

        public Task AddRangeAsync(IEnumerable<DepartmentModel> departments) => _lock.WriteAsync(async () =>
        {
            var c = departments.ToList();
            c.ForEach(_addDepartment);
            await _context.Departments.AddRangeAsync(c);
            await _context.SaveChangesAsync();
        });

        public async Task<Optional<DepartmentModel>> FromIdAsync(int id) =>
            await _lock.Read(() => Task.FromResult(_idToDepartment[id]));

        public async Task<Optional<DepartmentModel>> FromNameAsync(string name) =>
            await _lock.Read(() => Task.FromResult(_nameToDepartment[name]));

        public IEnumerable<DepartmentModel> Departments => _context.Departments;

        public IEnumerator<DepartmentModel> GetEnumerator() => Departments.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Departments.GetEnumerator();
    }
}