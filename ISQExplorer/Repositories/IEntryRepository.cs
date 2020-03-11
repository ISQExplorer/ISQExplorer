using System.Collections.Generic;
using System.Threading.Tasks;
using ISQExplorer.Functional;
using ISQExplorer.Models;

namespace ISQExplorer.Repositories
{
    public interface IEntryRepository : IEnumerable<ISQEntryModel>
    {
        Task<IEnumerable<ISQEntryModel>> ByCourseAsync(CourseModel course, TermModel? since = null, TermModel? until = null);
        Task<IEnumerable<ISQEntryModel>> ByProfessorAsync(ProfessorModel professor, TermModel? since = null, TermModel? until = null);
        Task AddAsync(ISQEntryModel professor);
        Task AddRangeAsync(IEnumerable<ISQEntryModel> professors);
        IEnumerable<ISQEntryModel> Entries { get; } 
    }
}