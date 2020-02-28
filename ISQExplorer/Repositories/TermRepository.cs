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

        private readonly Mutex writeMutex;
        private readonly Mutex readMutex;
        private int counter;

        public TermRepository()
        {
            _idToTerm = new Dictionary<int, TermModel>();
            _stringToTerm = new Dictionary<string, TermModel>();
            _ids = new SortedSet<int>();
            
            writeMutex = new Mutex();
            readMutex = new Mutex();
            counter = 0;
        }

        private T ReadOp<T>(Func<T> func)
        {
            readMutex.WaitOne();
            try
            {
                Interlocked.Increment(ref counter);
                if (counter == 1)
                {
                    writeMutex.WaitOne();
                }
            }
            finally
            {
                readMutex.ReleaseMutex();
            }

            Exception? ex = null;
            try
            {
                func();
            }
            catch (Exception e)
            {
                ex = e;
            }

            readMutex.WaitOne();
            try
            {
                Interlocked.Decrement(ref counter);
                if (counter == 0)
                {
                    writeMutex.ReleaseMutex();
                }
            }
            finally
            {
                readMutex.ReleaseMutex();
                if (ex != null)
                {
                    throw ex;
                }
            }
        }

        public void Add(TermModel term)
        {
            writeMutex.WaitOne();
            try
            {
                _idToTerm[term.Id] = term;
                _stringToTerm[term.Name] = term;
                _ids.Add(term.Id);
            }
            finally
            {
                writeMutex.ReleaseMutex();
            }
        }

        public Optional<TermModel> FromId(int id) =>
            ReadOp(() => _idToTerm.ContainsKey(id) ? _idToTerm[id] : new Optional<TermModel>());

        public Optional<TermModel> FromString(string str) =>
            ReadOp(() => _stringToTerm.ContainsKey(str) ? _stringToTerm[str] : new Optional<TermModel>());

        public Optional<TermModel> Previous(TermModel t, int howMany = 1) => ReadOp(() =>
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

        public Optional<TermModel> Next(TermModel t, int howMany = 1) => ReadOp(() =>
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