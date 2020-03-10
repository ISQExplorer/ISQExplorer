using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ISQExplorer.Functional;
using ISQExplorer.Misc;
using ISQExplorer.Models;

namespace ISQExplorer.Repositories
{
    public class ProfessorRepository : IProfessorRepository
    {
        private readonly DefaultDictionary<DepartmentModel, IDictionary<string, ProfessorModel>> _lastNameToProfessor;
        private readonly DefaultDictionary<DepartmentModel, IDictionary<string, ProfessorModel>> _firstNameToProfessor;
        private readonly DefaultDictionary<DepartmentModel, IDictionary<string, ProfessorModel>> _nameToProfessor;
        private readonly DefaultDictionary<DepartmentModel, IDictionary<string, ProfessorModel>> _nNumberToProfessor;
        private readonly ISQExplorerContext _context;

        private readonly DefaultDictionary<DepartmentModel, ReadWriteLock> _locks;

        private void _updateProf(ProfessorModel prof)
        {
            _lastNameToProfessor[prof.LastName] = prof;
            _firstNameToProfessor[prof.FirstName] = prof;
            _nameToProfessor[prof.FirstName + " " + prof.LastName] = prof;
            _nNumberToProfessor[prof.NNumber] = prof;
        }

        public ProfessorRepository(ISQExplorerContext context)
        {
            _lastNameToProfessor =
                new DefaultDictionary<DepartmentModel, IDictionary<string, ProfessorModel>>(() =>
                    new Dictionary<string, ProfessorModel>());
            _firstNameToProfessor =
                new DefaultDictionary<DepartmentModel, IDictionary<string, ProfessorModel>>(() =>
                    new Dictionary<string, ProfessorModel>());
            _nameToProfessor =
                new DefaultDictionary<DepartmentModel, IDictionary<string, ProfessorModel>>(() =>
                    new Dictionary<string, ProfessorModel>());
            _nNumberToProfessor =
                new DefaultDictionary<DepartmentModel, IDictionary<string, ProfessorModel>>(() =>
                    new Dictionary<string, ProfessorModel>());
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

        public async Task<Optional<ProfessorModel>> FromFirstName(DepartmentModel dept, string firstName) =>
            await Task.FromResult(_firstNameToProfessor[dept][firstName]);
    }