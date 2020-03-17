using System.Collections.Generic;
using System.Threading.Tasks;
using ISQExplorer.Functional;
using ISQExplorer.Models;

namespace ISQExplorer.Repositories
{
    public interface ICourseRepository : IEnumerable<CourseModel>
    {
        Task<Optional<CourseModel>> FromCourseCodeAsync(string courseCode);
        Task<Optional<CourseModel>> FromCourseNameAsync(string courseName);
        Task AddAsync(CourseModel course);
        Task AddRangeAsync(IEnumerable<CourseModel> courses);
        IEnumerable<CourseModel> Courses { get; }
        Task SaveChangesAsync();
    }
}