using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ISQExplorer.Functional;
using ISQExplorer.Misc;
using ISQExplorer.Models;

namespace ISQExplorer.Repositories
{
    public class CourseRepository : ICourseRepository
    {
        private readonly OptionalDictionary<string, CourseModel> _courseCodeToCourse;
        private readonly OptionalDictionary<string, CourseModel> _courseNameToCourse;
        private readonly ISet<string> _courseCodes;
        private readonly ISQExplorerContext _context;
        private readonly ReadWriteLock _lock;

        private void _addCourse(CourseModel course)
        {
            _courseCodeToCourse[course.CourseCode] = course;
            _courseNameToCourse[course.Name] = course;
            _courseCodes.Add(course.CourseCode);
        }

        public CourseRepository(ISQExplorerContext context)
        {
            _courseCodeToCourse = new OptionalDictionary<string, CourseModel>();
            _courseNameToCourse = new OptionalDictionary<string, CourseModel>();
            _courseCodes = new HashSet<string>();
            _context = context;
            _lock = new ReadWriteLock();
            
            _context.Courses.ForEach(_addCourse);
        }

        public Task AddAsync(CourseModel course) => _lock.Write(() =>
        {
            if (_courseCodes.Contains(course.CourseCode))
            {
                return Task.CompletedTask;
            }

            _addCourse(course);
            _context.Courses.Add(course);
            return Task.CompletedTask;
        });

        public Task AddRangeAsync(IEnumerable<CourseModel> courses) => _lock.Write(() =>
        {
            var c = courses.Where(c => !_courseCodes.Contains(c.CourseCode)).ToList();
            c.ForEach(_addCourse);
            _context.Courses.AddRange(c);
            return Task.CompletedTask;
        });

        public async Task<Optional<CourseModel>> FromCourseCodeAsync(string courseCode) =>
            await _lock.Read(() => Task.FromResult(_courseCodeToCourse[courseCode]));

        public async Task<Optional<CourseModel>> FromCourseNameAsync(string courseName) =>
            await _lock.Read(() => Task.FromResult(_courseNameToCourse[courseName]));

        public IEnumerable<CourseModel> Courses => _lock.Read(() => _courseCodeToCourse.Values.Values().ToList());

        public Task SaveChangesAsync() => _context.SaveChangesAsync();

        public IEnumerator<CourseModel> GetEnumerator() => Courses.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Courses.GetEnumerator();
    }
}