using System.Collections.Generic;
using ISQExplorer.Functional;
using ISQExplorer.Models;

namespace ISQExplorer.Repositories
{
    public interface ITermRepository
    {
        Optional<TermModel> FromId(int id);
        Optional<TermModel> FromString(string str);
        Optional<TermModel> Previous(TermModel t, int howMany = 0);
        Optional<TermModel> Next(TermModel t, int howMany = 0);
        void Add(TermModel term);
        void AddRange(IEnumerable<TermModel> term);
    }
}