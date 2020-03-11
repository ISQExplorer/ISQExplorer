using System.Collections;
using System.Collections.Concurrent;
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
        private readonly IDictionary<string, CourseModel> _courseCodeToCourse;
        private readonly IDictionary<string, CourseModel> _courseNameToCourse;
        private readonly ISQExplorerContext _context;
        private readonly ReadWriteLock _lock;

        private void _addCourse(CourseModel course)
        {
            _courseCodeToCourse[course.CourseCode] = course;
            _courseNameToCourse[course.Name] = course;
        }

        public CourseRepository(ISQExplorerContext context)
        {
            _courseCodeToCourse = new Dictionary<string, CourseModel>();
            _courseNameToCourse = new Dictionary<string, CourseModel>();
            _context = context;
            _lock = new ReadWriteLock();
        }

        public Task AddAsync(CourseModel course) => _lock.WriteAsync(async () =>
        {
            _addCourse(course);
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();
        });

        public Task AddRangeAsync(IEnumerable<CourseModel> courses) => _lock.WriteAsync(async () =>
        {
            var c = courses.ToList();
            c.ForEach(_addCourse);
            await _context.Courses.AddRangeAsync(c);
            await _context.SaveChangesAsync();
        });

        public async Task<Optional<CourseModel>> FromCourseCodeAsync(string courseCode) =>
            await _lock.Read(() => Task.FromResult(_courseCodeToCourse[courseCode]));

        public async Task<Optional<CourseModel>> FromCourseNameAsync(string courseName) =>
            await _lock.Read(() => Task.FromResult(_courseNameToCourse[courseName]));

        public IEnumerable<CourseModel> Courses => _context.Courses;

        public IEnumerator<CourseModel> GetEnumerator() => Courses.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Courses.GetEnumerator();
    }
}