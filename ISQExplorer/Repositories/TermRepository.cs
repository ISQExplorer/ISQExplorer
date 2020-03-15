using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ISQExplorer.Functional;
using ISQExplorer.Misc;
using ISQExplorer.Models;

namespace ISQExplorer.Repositories
{
    public class TermRepository : ITermRepository
    {
        private readonly OptionalDictionary<int, TermModel> _idToTerm;
        private readonly OptionalDictionary<string, TermModel> _stringToTerm;
        private readonly SortedSet<int> _ids;
        private readonly ISQExplorerContext _context;

        private readonly ReadWriteLock _lock;

        public TermRepository(ISQExplorerContext context)
        {
            _idToTerm = new OptionalDictionary<int, TermModel>();
            _stringToTerm = new OptionalDictionary<string, TermModel>();
            _ids = new SortedSet<int>();
            _context = context;

            _lock = new ReadWriteLock();

            _lock.Read(() => _context.Terms.ForEach(term =>
            {
                _idToTerm[term.Id] = term;
                _stringToTerm[term.Name] = term;
                _ids.Add(term.Id);
            }));
        }

        public Task AddAsync(TermModel term) => _lock.Write(async () =>
        {
            _idToTerm[term.Id] = term;
            _stringToTerm[term.Name] = term;
            _ids.Add(term.Id);
            await _context.Terms.AddAsync(term);
        });

        public Task AddRangeAsync(IEnumerable<TermModel> terms) => _lock.Write(async () =>
        {
            var t = terms.ToList();

            foreach (var term in t)
            {
                _idToTerm[term.Id] = term;
                _stringToTerm[term.Name] = term;
                _ids.Add(term.Id);
            }

            await _context.Terms.AddRangeAsync(t);
        });

        public async Task<Optional<TermModel>> FromIdAsync(int id) =>
            await Task.FromResult(_lock.Read(() => _idToTerm.ContainsKey(id) ? _idToTerm[id] : new Optional<TermModel>()));

        public async Task<Optional<TermModel>> FromStringAsync(string str) =>
            await Task.FromResult(_lock.Read(() => _stringToTerm.ContainsKey(str) ? _stringToTerm[str] : new Optional<TermModel>()));

        public Task<Optional<TermModel>> PreviousAsync(TermModel t, int howMany = 1) => _lock.Read(() =>
        {
            if (howMany < 0)
            {
                return NextAsync(t, -howMany);
            }

            var index = _ids.Index(t.Id);
            return Task.FromResult(index - howMany < 0
                ? new Optional<TermModel>()
                : _idToTerm[_ids.ElementAt(index - howMany)]);
        });

        public Task<Optional<TermModel>> NextAsync(TermModel t, int howMany = 1) => _lock.Read(() =>
        {
            if (howMany < 0)
            {
                return PreviousAsync(t, -howMany);
            }

            var index = _ids.Index(t.Id);
            return Task.FromResult(index + howMany >= _ids.Count
                ? new Optional<TermModel>()
                : _idToTerm[_ids.ElementAt(index + howMany)]);
        });

        public IEnumerable<TermModel> Terms => _ids.Select(id => _idToTerm[id]).Values();

        public IEnumerator GetEnumerator() => Terms.GetEnumerator();

        IEnumerator<TermModel> IEnumerable<TermModel>.GetEnumerator() => Terms.GetEnumerator();
    }
}