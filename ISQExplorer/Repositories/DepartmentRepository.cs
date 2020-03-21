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
        private readonly OptionalDictionary<int, DepartmentModel> _idToDepartment;
        private readonly OptionalDictionary<string, DepartmentModel> _nameToDepartment;
        private readonly ISet<int> _deptIds;
        private readonly ISQExplorerContext _context;
        private readonly ReadWriteLock _lock;

        private void _addDepartment(DepartmentModel department)
        {
            _idToDepartment[department.Id] = department;
            _nameToDepartment[department.Name] = department;
            _deptIds.Add(department.Id);
        }

        public DepartmentRepository(ISQExplorerContext context)
        {
            _idToDepartment = new OptionalDictionary<int, DepartmentModel>();
            _nameToDepartment = new OptionalDictionary<string, DepartmentModel>();
            _deptIds = new HashSet<int>();
            _context = context;
            _lock = new ReadWriteLock();
        }

        public Task AddAsync(DepartmentModel department) => _lock.Write(() =>
        {
            if (_deptIds.Contains(department.Id))
            {
                return Task.CompletedTask;
            }
            
            _addDepartment(department);
            _context.Departments.Add(department);
            return Task.CompletedTask;
        });

        public Task AddRangeAsync(IEnumerable<DepartmentModel> departments) => _lock.Write(() =>
        {
            var c = departments.Where(d => !_deptIds.Contains(d.Id)).ToList();
            c.ForEach(_addDepartment);
            _context.Departments.AddRange(c);
            return Task.CompletedTask;
        });

        public async Task<Optional<DepartmentModel>> FromIdAsync(int id) =>
            await _lock.Read(() => Task.FromResult(_idToDepartment[id]));

        public async Task<Optional<DepartmentModel>> FromNameAsync(string name) =>
            await _lock.Read(() => Task.FromResult(_nameToDepartment[name]));

        public IEnumerable<DepartmentModel> Departments => _lock.Read(() =>_idToDepartment
            .Values.Values().ToList());

        public Task SaveChangesAsync() => _context.SaveChangesAsync();

        public IEnumerator<DepartmentModel> GetEnumerator() => Departments.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Departments.GetEnumerator();
    }
}