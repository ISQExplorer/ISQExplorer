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
        private readonly DefaultDictionary<DepartmentModel, OptionalDictionary<string, ProfessorModel>> _lastNameToProfessor;
        private readonly DefaultDictionary<DepartmentModel, OptionalDictionary<string, ProfessorModel>> _firstNameToProfessor;
        private readonly DefaultDictionary<DepartmentModel, OptionalDictionary<string, ProfessorModel>> _nameToProfessor;
        private readonly DefaultDictionary<DepartmentModel, OptionalDictionary<string, ProfessorModel>> _nNumberToProfessor;
        private readonly ISQExplorerContext _context;

        private readonly DefaultDictionary<DepartmentModel, ReadWriteLock> _locks;

        private void _updateProf(ProfessorModel prof)
        {
            _lastNameToProfessor[prof.Department][prof.LastName] = prof;
            _firstNameToProfessor[prof.Department][prof.FirstName] = prof;
            _nameToProfessor[prof.Department][prof.FirstName + " " + prof.LastName] = prof;
            _nNumberToProfessor[prof.Department][prof.NNumber] = prof;
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
            _locks = new DefaultDictionary<DepartmentModel, ReadWriteLock>(() => new ReadWriteLock());
            _context = context;

            _context.Professors.ForEach(_updateProf);
        }

        public Task AddAsync(ProfessorModel prof) => _locks[prof.Department].Write(async () =>
        {
            _updateProf(prof);
            _context.Add(prof);
            await _context.SaveChangesAsync();
        });

        public async Task AddRangeAsync(IEnumerable<ProfessorModel> profs)
        {
            var pr = profs.ToList();
            var byDept = pr.GroupBy(x => x.Department);
            await Task.WhenAll(byDept.Select(x => Task.Run(async () =>
            {
                await _locks[x.Key].Write(async () =>
                {
                    x.ForEach(_updateProf);
                    await _context.Professors.AddRangeAsync();
                });
            })));
            await _context.SaveChangesAsync();
        }

        public async Task<Optional<ProfessorModel>> FromFirstNameAsync(DepartmentModel dept, string firstName) =>
            await Task.FromResult(_locks[dept].Read(() => _firstNameToProfessor[dept][firstName]));

        public async Task<Optional<ProfessorModel>> FromLastNameAsync(DepartmentModel dept, string lastName) =>
            await Task.FromResult(_locks[dept].Read(() => _lastNameToProfessor[dept][lastName]));

        public async Task<Optional<ProfessorModel>> FromNameAsync(DepartmentModel dept, string name) =>
            await Task.FromResult(_locks[dept].Read(() => _nameToProfessor[dept][name]));

        public async Task<Optional<ProfessorModel>> FromNNumberAsync(DepartmentModel dept, string nNumber) =>
            await Task.FromResult(_locks[dept].Read(() => _nNumberToProfessor[dept][nNumber]));

        public IEnumerable<ProfessorModel> Professors => _context.Professors;

        public IEnumerator<ProfessorModel> GetEnumerator() => Professors.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Professors.GetEnumerator();
    }
}