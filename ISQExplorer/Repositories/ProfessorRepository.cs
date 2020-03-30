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
    internal class ProfessorInfo
    {
         public readonly DefaultDictionary<DepartmentModel, OptionalDictionary<string, ProfessorModel>>
             LastNameToProfessor;
         public readonly DefaultDictionary<DepartmentModel, OptionalDictionary<string, ProfessorModel>>
             FirstNameToProfessor;
         public readonly DefaultDictionary<DepartmentModel, OptionalDictionary<string, ProfessorModel>>
             NameToProfessor;
         public readonly DefaultDictionary<DepartmentModel, OptionalDictionary<string, ProfessorModel>>
             NNumberToProfessor;
         public readonly ISet<string> NNumbers;
         public readonly ReadWriteLock Lock;
         
         public static readonly ProfessorInfo Instance = new ProfessorInfo();

         private ProfessorInfo()
         {
             LastNameToProfessor =
                 new DefaultDictionary<DepartmentModel, OptionalDictionary<string, ProfessorModel>>(() =>
                     new OptionalDictionary<string, ProfessorModel>());
             FirstNameToProfessor =
                 new DefaultDictionary<DepartmentModel, OptionalDictionary<string, ProfessorModel>>(() =>
                     new OptionalDictionary<string, ProfessorModel>());
             NameToProfessor =
                 new DefaultDictionary<DepartmentModel, OptionalDictionary<string, ProfessorModel>>(() =>
                     new OptionalDictionary<string, ProfessorModel>());
             NNumberToProfessor =
                 new DefaultDictionary<DepartmentModel, OptionalDictionary<string, ProfessorModel>>(() =>
                     new OptionalDictionary<string, ProfessorModel>());
             NNumbers = new HashSet<string>();
             Lock = new ReadWriteLock();
         }
    }
    
    public class ProfessorRepository : IProfessorRepository
    {
        private readonly ProfessorInfo _info;
        private readonly ISQExplorerContext _context;

        private void _updateProf(ProfessorModel prof)
        {
            _info.LastNameToProfessor[prof.Department][prof.LastName] = prof;
            _info.FirstNameToProfessor[prof.Department][prof.FirstName] = prof;
            _info.NameToProfessor[prof.Department][prof.FirstName + " " + prof.LastName] = prof;
            _info.NNumberToProfessor[prof.Department][prof.NNumber] = prof;
            _info.NNumbers.Add(prof.NNumber);
        }

        public ProfessorRepository(ISQExplorerContext context)
        {
            _info = ProfessorInfo.Instance;
            _context = context;

            _info.Lock.Write(() =>
            {
                if (_info.NNumbers.None())
                {
                    _context.Professors.ForEach(_updateProf);
                }
            });
        }

        public Task AddAsync(ProfessorModel prof) => _info.Lock.Write(() =>
        {
            if (_info.NNumbers.Contains(prof.NNumber))
            {
                return Task.CompletedTask;
            }
            
            _updateProf(prof);
            _context.Add(prof);
            return Task.CompletedTask;
        });

        public Task AddRangeAsync(IEnumerable<ProfessorModel> profs) => _info.Lock.Write(() =>
        {
            var pr = profs.Where(p => !_info.NNumbers.Contains(p.NNumber)).ToList();
            pr.ForEach(_updateProf);
            _context.AddRange(pr);
            return Task.CompletedTask;
        });

        public async Task<Optional<ProfessorModel>> FromFirstNameAsync(DepartmentModel dept, string firstName) =>
            await Task.FromResult(_info.Lock.Read(() => _info.FirstNameToProfessor[dept][firstName]));

        public async Task<Optional<ProfessorModel>> FromLastNameAsync(DepartmentModel dept, string lastName) =>
            await Task.FromResult(_info.Lock.Read(() => _info.LastNameToProfessor[dept][lastName]));

        public async Task<Optional<ProfessorModel>> FromNameAsync(DepartmentModel dept, string name) =>
            await Task.FromResult(_info.Lock.Read(() => _info.NameToProfessor[dept][name]));

        public async Task<Optional<ProfessorModel>> FromNNumberAsync(DepartmentModel dept, string nNumber) =>
            await Task.FromResult(_info.Lock.Read(() => _info.NNumberToProfessor[dept][nNumber]));

        public IEnumerable<ProfessorModel> Professors => _info.NNumberToProfessor.Values.SelectMany(x => x.Values.Values(),
            (_, y) => y);

        public Task SaveChangesAsync() => _context.SaveChangesAsync();

        public IEnumerator<ProfessorModel> GetEnumerator() => Professors.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Professors.GetEnumerator();
    }
}