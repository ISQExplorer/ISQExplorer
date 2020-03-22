using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ISQExplorer.Functional;
using ISQExplorer.Misc;
using ISQExplorer.Models;

// below comment is used to suppress resharper warning that the defaultdict entries are never initialized
// ReSharper disable CollectionNeverUpdated.Local

namespace ISQExplorer.Repositories
{
    public class ProfessorRepository : IProfessorRepository
    {
        private readonly DefaultDictionary<DepartmentModel, OptionalDictionary<string, ProfessorModel>>
            _lastNameToProfessor;

        private readonly DefaultDictionary<DepartmentModel, OptionalDictionary<string, ProfessorModel>>
            _firstNameToProfessor;

        private readonly DefaultDictionary<DepartmentModel, OptionalDictionary<string, ProfessorModel>>
            _nameToProfessor;

        private readonly DefaultDictionary<DepartmentModel, OptionalDictionary<string, ProfessorModel>>
            _nNumberToProfessor;
        
        private readonly ISet<string> _nNumbers;

        private readonly ISQExplorerContext _context;

        private readonly ReadWriteLock _lock;

        private void _updateProf(ProfessorModel prof)
        {
            _lastNameToProfessor[prof.Department][prof.LastName] = prof;
            _firstNameToProfessor[prof.Department][prof.FirstName] = prof;
            _nameToProfessor[prof.Department][prof.FirstName + " " + prof.LastName] = prof;
            _nNumberToProfessor[prof.Department][prof.NNumber] = prof;
            _nNumbers.Add(prof.NNumber);
        }

        public ProfessorRepository(ISQExplorerContext context)
        {
            _lastNameToProfessor =
                new DefaultDictionary<DepartmentModel, OptionalDictionary<string, ProfessorModel>>(() =>
                    new OptionalDictionary<string, ProfessorModel>());
            _firstNameToProfessor =
                new DefaultDictionary<DepartmentModel, OptionalDictionary<string, ProfessorModel>>(() =>
                    new OptionalDictionary<string, ProfessorModel>());
            _nameToProfessor =
                new DefaultDictionary<DepartmentModel, OptionalDictionary<string, ProfessorModel>>(() =>
                    new OptionalDictionary<string, ProfessorModel>());
            _nNumberToProfessor =
                new DefaultDictionary<DepartmentModel, OptionalDictionary<string, ProfessorModel>>(() =>
                    new OptionalDictionary<string, ProfessorModel>());
            _nNumbers = new HashSet<string>();
            _lock = new ReadWriteLock();
            _context = context;

            _context.Professors.ForEach(_updateProf);
        }

        public Task AddAsync(ProfessorModel prof) => _lock.Write(() =>
        {
            if (_nNumbers.Contains(prof.NNumber))
            {
                return Task.CompletedTask;
            }
            
            _updateProf(prof);
            _context.Add(prof);
            return Task.CompletedTask;
        });

        public Task AddRangeAsync(IEnumerable<ProfessorModel> profs) => _lock.Write(() =>
        {
            var pr = profs.Where(p => !_nNumbers.Contains(p.NNumber)).ToList();
            pr.ForEach(_updateProf);
            _context.AddRange(pr);
            return Task.CompletedTask;
        });

        public async Task<Optional<ProfessorModel>> FromFirstNameAsync(DepartmentModel dept, string firstName) =>
            await Task.FromResult(_lock.Read(() => _firstNameToProfessor[dept][firstName]));

        public async Task<Optional<ProfessorModel>> FromLastNameAsync(DepartmentModel dept, string lastName) =>
            await Task.FromResult(_lock.Read(() => _lastNameToProfessor[dept][lastName]));

        public async Task<Optional<ProfessorModel>> FromNameAsync(DepartmentModel dept, string name) =>
            await Task.FromResult(_lock.Read(() => _nameToProfessor[dept][name]));

        public async Task<Optional<ProfessorModel>> FromNNumberAsync(DepartmentModel dept, string nNumber) =>
            await Task.FromResult(_lock.Read(() => _nNumberToProfessor[dept][nNumber]));

        public IEnumerable<ProfessorModel> Professors => _nNumberToProfessor.Values.SelectMany(x => x.Values.Values(),
            (_, y) => y);

        public Task SaveChangesAsync() => _context.SaveChangesAsync();

        public IEnumerator<ProfessorModel> GetEnumerator() => Professors.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Professors.GetEnumerator();
    }
}