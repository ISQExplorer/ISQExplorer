using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ISQExplorer.Functional;
using ISQExplorer.Misc;
using ISQExplorer.Models;

namespace ISQExplorer.Repositories
{
    internal class DepartmentInfo
    {
         public readonly OptionalDictionary<int, DepartmentModel> IdToDepartment;
         public readonly OptionalDictionary<string, DepartmentModel> NameToDepartment;
         public readonly ISet<int> DeptIds;
         public readonly ReadWriteLock Lock;

         public static readonly DepartmentInfo Instance = new DepartmentInfo();

         private DepartmentInfo()
         {
             IdToDepartment = new OptionalDictionary<int, DepartmentModel>();
             NameToDepartment = new OptionalDictionary<string, DepartmentModel>();
             DeptIds = new HashSet<int>();
             Lock = new ReadWriteLock();            
         }
    }
    
    public class DepartmentRepository : IDepartmentRepository
    {
        private readonly DepartmentInfo _info;
        private readonly ISQExplorerContext _context;

        private void _addDepartment(DepartmentModel department)
        {
            _info.IdToDepartment[department.Id] = department;
            _info.NameToDepartment[department.Name] = department;
            _info.DeptIds.Add(department.Id);
        }

        public DepartmentRepository(ISQExplorerContext context)
        {
            _context = context;
            _info = DepartmentInfo.Instance;
           
            _info.Lock.Write(() =>
            {
                if (_info.DeptIds.None())
                {
                    context.Departments.ForEach(_addDepartment);
                }
            });
        }

        public Task AddAsync(DepartmentModel department) => _info.Lock.Write(() =>
        {
            if (_info.DeptIds.Contains(department.Id))
            {
                return Task.CompletedTask;
            }
            
            _addDepartment(department);
            _context.Departments.Add(department);
            return Task.CompletedTask;
        });

        public Task AddRangeAsync(IEnumerable<DepartmentModel> departments) => _info.Lock.Write(() =>
        {
            var c = departments.Where(d => !_info.DeptIds.Contains(d.Id)).ToList();
            c.ForEach(_addDepartment);
            _context.Departments.AddRange(c);
            return Task.CompletedTask;
        });

        public async Task<Optional<DepartmentModel>> FromIdAsync(int id) =>
            await _info.Lock.Read(() => Task.FromResult(_info.IdToDepartment[id]));

        public async Task<Optional<DepartmentModel>> FromNameAsync(string name) =>
            await _info.Lock.Read(() => Task.FromResult(_info.NameToDepartment[name]));

        public IEnumerable<DepartmentModel> Departments => _info.Lock.Read(() => _info.IdToDepartment
            .Values.Values().ToList());

        public Task SaveChangesAsync() => _context.SaveChangesAsync();

        public IEnumerator<DepartmentModel> GetEnumerator() => Departments.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Departments.GetEnumerator();
    }
}