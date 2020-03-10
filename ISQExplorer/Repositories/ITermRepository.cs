using System.Collections.Generic;
using System.Threading.Tasks;
using ISQExplorer.Functional;
using ISQExplorer.Models;

namespace ISQExplorer.Repositories
{
    public interface ITermRepository : IEnumerable<TermModel>
    {
        Task<Optional<TermModel>> FromIdAsync(int id);
        Task<Optional<TermModel>> FromStringAsync(string str);
        Task<Optional<TermModel>> PreviousAsync(TermModel t, int howMany = 0);
        Task<Optional<TermModel>> NextAsync(TermModel t, int howMany = 0);
        Task AddAsync(TermModel term);
        Task AddRangeAsync(IEnumerable<TermModel> term);
        IEnumerable<TermModel> Terms { get; }
    }
}