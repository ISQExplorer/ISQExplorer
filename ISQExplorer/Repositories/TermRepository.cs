using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ISQExplorer.Functional;
using ISQExplorer.Misc;
using ISQExplorer.Models;

namespace ISQExplorer.Repositories
{
    internal class TermInfo
    {
        public readonly OptionalDictionary<int, TermModel> IdToTerm;
        public readonly OptionalDictionary<string, TermModel> StringToTerm;
        public readonly SortedSet<int> Ids;
        public readonly HashSet<int> IdHashSet;
        public readonly ReadWriteLock Lock;

        public static readonly TermInfo Instance = new TermInfo();

        private TermInfo()
        {
            IdToTerm = new OptionalDictionary<int, TermModel>();
            StringToTerm = new OptionalDictionary<string, TermModel>();
            Ids = new SortedSet<int>();
            IdHashSet = new HashSet<int>();

            Lock = new ReadWriteLock();
        }
    }

    public class TermRepository : ITermRepository
    {
        private readonly TermInfo _info;
        private readonly ISQExplorerContext _context;

        private void _addTerm(TermModel term)
        {
            _info.IdToTerm[term.Id] = term;
            _info.StringToTerm[term.Name] = term;
            _info.Ids.Add(term.Id);
            _info.IdHashSet.Add(term.Id);
        }

        public TermRepository(ISQExplorerContext context)
        {
            _info = TermInfo.Instance;
            _context = context;

            _info.Lock.Write(() =>
            {
                if (_info.IdHashSet.None())
                {
                    _context.Terms.ForEach(_addTerm);
                }
            });
        }

        public Task AddAsync(TermModel term) => _info.Lock.Write(() =>
        {
            if (_info.IdHashSet.Contains(term.Id))
            {
                return Task.CompletedTask;
            }

            _addTerm(term);
            _context.Terms.AddAsync(term);
            return Task.CompletedTask;
        });

        public Task AddRangeAsync(IEnumerable<TermModel> terms) => _info.Lock.Write(() =>
        {
            var t = terms.Where(ter => !_info.IdHashSet.Contains(ter.Id)).ToList();
            t.ForEach(_addTerm);
            _context.Terms.AddRange(t);
            return Task.CompletedTask;
        });

        public async Task<Optional<TermModel>> FromIdAsync(int id) =>
            await Task.FromResult(
                _info.Lock.Read(() => _info.IdToTerm.ContainsKey(id) ? _info.IdToTerm[id] : new Optional<TermModel>()));

        public async Task<Optional<TermModel>> FromStringAsync(string str) =>
            await Task.FromResult(_info.Lock.Read(() =>
                _info.StringToTerm.ContainsKey(str) ? _info.StringToTerm[str] : new Optional<TermModel>()));

        public Task<Optional<TermModel>> PreviousAsync(TermModel t, int howMany = 1) => _info.Lock.Read(() =>
        {
            if (howMany < 0)
            {
                return NextAsync(t, -howMany);
            }

            var index = _info.Ids.Index(t.Id);
            return Task.FromResult(index - howMany < 0
                ? new Optional<TermModel>()
                : _info.IdToTerm[_info.Ids.ElementAt(index - howMany)]);
        });

        public Task<Optional<TermModel>> NextAsync(TermModel t, int howMany = 1) => _info.Lock.Read(() =>
        {
            if (howMany < 0)
            {
                return PreviousAsync(t, -howMany);
            }

            var index = _info.Ids.Index(t.Id);
            return Task.FromResult(index + howMany >= _info.Ids.Count
                ? new Optional<TermModel>()
                : _info.IdToTerm[_info.Ids.ElementAt(index + howMany)]);
        });

        public IEnumerable<TermModel> Terms => _info.Ids.Select(id => _info.IdToTerm[id]).Values();

        public Task SaveChangesAsync() => _context.SaveChangesAsync();

        public IEnumerator GetEnumerator() => Terms.GetEnumerator();

        IEnumerator<TermModel> IEnumerable<TermModel>.GetEnumerator() => Terms.GetEnumerator();
    }
}