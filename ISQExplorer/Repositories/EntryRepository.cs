using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ISQExplorer.Functional;
using ISQExplorer.Misc;
using ISQExplorer.Models;
using Microsoft.EntityFrameworkCore.Internal;

// ReSharper disable CollectionNeverUpdated.Local

namespace ISQExplorer.Repositories
{
    public class EntryRepository : IEntryRepository
    {
        private readonly DefaultDictionary<CourseModel, ISet<ISQEntryModel>> _courseToEntries;
        private readonly DefaultDictionary<ProfessorModel, ISet<ISQEntryModel>> _professorToEntries;
        private readonly ISet<ISQEntryModel> _entries;
        private readonly ISQExplorerContext _context;
        private readonly ReadWriteLock _lock;

        private void _addEntry(ISQEntryModel entry)
        {
            _courseToEntries[entry.Course].Add(entry);
            _professorToEntries[entry.Professor].Add(entry);
            _entries.Add(entry);
        }

        public EntryRepository(ISQExplorerContext context)
        {
            _courseToEntries =
                new DefaultDictionary<CourseModel, ISet<ISQEntryModel>>(() => new HashSet<ISQEntryModel>());
            _professorToEntries =
                new DefaultDictionary<ProfessorModel, ISet<ISQEntryModel>>(() => new HashSet<ISQEntryModel>());
            _entries = new HashSet<ISQEntryModel>();
            _context = context;
            _lock = new ReadWriteLock();
        }

        public Task AddAsync(ISQEntryModel entry) => _lock.Write(() =>
        {
            if (_entries.Contains(entry))
            {
                return Task.CompletedTask;
            }
            
            _addEntry(entry);
            _context.IsqEntries.Add(entry);
            return Task.CompletedTask;
        });

        public Task AddRangeAsync(IEnumerable<ISQEntryModel> entries) => _lock.Write(() =>
        {
            var e = entries.Where(x => !_entries.Contains(x)).ToList();
            e.ForEach(_addEntry);
            _context.IsqEntries.AddRange(e);
            return Task.CompletedTask;
        });

        public async Task<IEnumerable<ISQEntryModel>> ByCourseAsync(CourseModel course, TermModel? since = null,
            TermModel? until = null) => await _lock.Read(() =>
            Task.FromResult(_context.IsqEntries.Where(x => x.Course == course).When(since, until)));

        public async Task<IEnumerable<ISQEntryModel>> ByProfessorAsync(ProfessorModel professor,
            TermModel? since = null,
            TermModel? until = null) =>
            await _lock.Read(() =>
                Task.FromResult(_context.IsqEntries.Where(x => x.Professor == professor).When(since, until)));

        public IEnumerable<ISQEntryModel> Entries =>
            _lock.Read(() => _courseToEntries.Values.SelectMany(x => x).ToList());

        public Task SaveChangesAsync() => _context.SaveChangesAsync();

        public IEnumerator<ISQEntryModel> GetEnumerator() => Entries.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Entries.GetEnumerator();
    }
}