using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ISQExplorer.Misc;
using ISQExplorer.Models;

// ReSharper disable CollectionNeverUpdated.Local

namespace ISQExplorer.Repositories
{
    internal class EntryInfo
    {
        public readonly DefaultDictionary<CourseModel, ISet<ISQEntryModel>> CourseToEntries;
        public readonly DefaultDictionary<ProfessorModel, ISet<ISQEntryModel>> ProfessorToEntries;
        public readonly ISet<ISQEntryModel> Entries;
        public readonly ReadWriteLock Lock;
        
        public static readonly EntryInfo Instance = new EntryInfo();

        private EntryInfo()
        {
             CourseToEntries =
                 new DefaultDictionary<CourseModel, ISet<ISQEntryModel>>(() => new HashSet<ISQEntryModel>());
             ProfessorToEntries =
                 new DefaultDictionary<ProfessorModel, ISet<ISQEntryModel>>(() => new HashSet<ISQEntryModel>());
             Entries = new HashSet<ISQEntryModel>();
             Lock = new ReadWriteLock();           
        }
    }
    
    public class EntryRepository : IEntryRepository
    {
        private readonly EntryInfo _info;
        private readonly ISQExplorerContext _context;

        private void _addEntry(ISQEntryModel entry)
        {
            _info.CourseToEntries[entry.Course].Add(entry);
            _info.ProfessorToEntries[entry.Professor].Add(entry);
            _info.Entries.Add(entry);
        }

        public EntryRepository(ISQExplorerContext context)
        {
            _context = context;
            _info = EntryInfo.Instance;

            _info.Lock.Write(() =>
            {
                if (_info.Entries.None())
                {
                    _context.IsqEntries.ForEach(_addEntry);
                }
            });
        }

        public Task AddAsync(ISQEntryModel entry) => _info.Lock.Write(() =>
        {
            if (_info.Entries.Contains(entry))
            {
                return Task.CompletedTask;
            }
            
            _addEntry(entry);
            _context.IsqEntries.Add(entry);
            return Task.CompletedTask;
        });

        public Task AddRangeAsync(IEnumerable<ISQEntryModel> entries) => _info.Lock.Write(() =>
        {
            var e = entries.Where(x => !_info.Entries.Contains(x)).ToList();
            e.ForEach(_addEntry);
            _context.IsqEntries.AddRange(e);
            return Task.CompletedTask;
        });

        public async Task<IEnumerable<ISQEntryModel>> ByCourseAsync(CourseModel course, TermModel? since = null,
            TermModel? until = null) => await _info.Lock.Read(() =>
            Task.FromResult(_context.IsqEntries.Where(x => x.Course == course).When(since, until)));

        public async Task<IEnumerable<ISQEntryModel>> ByProfessorAsync(ProfessorModel professor,
            TermModel? since = null,
            TermModel? until = null) =>
            await _info.Lock.Read(() =>
                Task.FromResult(_context.IsqEntries.Where(x => x.Professor == professor).When(since, until)));

        public IEnumerable<ISQEntryModel> Entries =>
            _info.Lock.Read(() => _info.CourseToEntries.Values.SelectMany(x => x).ToList());

        public IQueryable<ISQEntryModel> AsQueryable() => _context.IsqEntries;

        public Task SaveChangesAsync() => _context.SaveChangesAsync();

        public IEnumerator<ISQEntryModel> GetEnumerator() => Entries.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Entries.GetEnumerator();
    }
}