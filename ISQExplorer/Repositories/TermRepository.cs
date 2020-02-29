using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ISQExplorer.Functional;
using ISQExplorer.Misc;
using ISQExplorer.Models;

namespace ISQExplorer.Repositories
{
    public class TermRepository : ITermRepository
    {
        private readonly IDictionary<int, TermModel> _idToTerm;
        private readonly IDictionary<string, TermModel> _stringToTerm;
        private readonly SortedSet<int> _ids;

        private readonly ReadWriteLock _lock;

        public TermRepository()
        {
            _idToTerm = new Dictionary<int, TermModel>();
            _stringToTerm = new Dictionary<string, TermModel>();
            _ids = new SortedSet<int>();

            _lock = new ReadWriteLock();
        }

        public void Add(TermModel term) => _lock.Write(() =>
        {
            _idToTerm[term.Id] = term;
            _stringToTerm[term.Name] = term;
            _ids.Add(term.Id);
        });

        public void AddRange(IEnumerable<TermModel> terms) => _lock.Write(() =>
        {
            foreach (var term in terms)
            {
                _idToTerm[term.Id] = term;
                _stringToTerm[term.Name] = term;
                _ids.Add(term.Id);
            }
        });

        public Optional<TermModel> FromId(int id) =>
            _lock.Read(() => _idToTerm.ContainsKey(id) ? _idToTerm[id] : new Optional<TermModel>());

        public Optional<TermModel> FromString(string str) =>
            _lock.Read(() => _stringToTerm.ContainsKey(str) ? _stringToTerm[str] : new Optional<TermModel>());

        public Optional<TermModel> Previous(TermModel t, int howMany = 1) => _lock.Read(() =>
        {
            if (howMany < 0)
            {
                return Next(t, -howMany);
            }

            var index = _ids.Index(t.Id);
            return index - howMany < 0
                ? new Optional<TermModel>()
                : _idToTerm[_ids.ElementAt(index - howMany)];
        });

        public Optional<TermModel> Next(TermModel t, int howMany = 1) => _lock.Read(() =>
        {
            if (howMany < 0)
            {
                return Next(t, -howMany);
            }

            var index = _ids.Index(t.Id);
            return index + howMany >= _ids.Count
                ? new Optional<TermModel>()
                : _idToTerm[_ids.ElementAt(index + howMany)];
        });
    }
}