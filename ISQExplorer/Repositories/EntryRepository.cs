using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ISQExplorer.Functional;
using ISQExplorer.Misc;
using ISQExplorer.Models;

namespace ISQExplorer.Repositories
{
    public class EntryRepository : IEntryRepository
    {
        private DefaultDictionary<CourseModel, ISet<ISQEntryModel>> _courseToEntries;
        private DefaultDictionary<ProfessorModel, ISet<ISQEntryModel>> _professorToEntries;
        private readonly ISQExplorerContext _context;
        private readonly ReadWriteLock _lock;

        private void _addEntity(ISQEntryModel entry)
        {
            _courseToEntries[entry.Course].Add(entry);
            _professorToEntries[entry.Professor].Add(entry);
        }

        public EntryRepository(ISQExplorerContext context)
        {
            _courseToEntries =
                new DefaultDictionary<CourseModel, ISet<ISQEntryModel>>(() => new HashSet<ISQEntryModel>());
            _professorToEntries =
                new DefaultDictionary<ProfessorModel, ISet<ISQEntryModel>>(() => new HashSet<ISQEntryModel>());
            _context = context;
            _lock = new ReadWriteLock();
        }

        public Task AddAsync(ISQEntryModel entry) => _lock.WriteAsync(async () =>
        {
            _addEntity(entry);
            _context.IsqEntries.Add(entry);
            await _context.SaveChangesAsync();
        });

        public Task AddRangeAsync(IEnumerable<ISQEntryModel> entries) => _lock.WriteAsync(async () =>
        {
            var e = entries.ToList();
            e.ForEach(_addEntity);
            await _context.IsqEntries.AddRangeAsync(e);
            await _context.SaveChangesAsync();
        });

        public async Task<IEnumerable<ISQEntryModel>> ByCourseAsync(CourseModel course, TermModel since = null,
            TermModel until = null) => await _lock.Read(() =>
            Task.FromResult(_context.IsqEntries.Where(x => x.Course == course).When(since, until)));

        public async Task<IEnumerable<ISQEntryModel>> ByProfessorAsync(ProfessorModel professor, TermModel since = null,
            TermModel until = null) =>
            await _lock.Read(() =>
                Task.FromResult(_context.IsqEntries.Where(x => x.Professor == professor).When(since, until)));

        public IEnumerable<ISQEntryModel> Entries => _context.IsqEntries;

        public IEnumerator<ISQEntryModel> GetEnumerator() => Entries.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Entries.GetEnumerator();
    }
}