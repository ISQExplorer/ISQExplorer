using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ISQExplorer.Functional;
using ISQExplorer.Misc;
using ISQExplorer.Models;

namespace ISQExplorer.Repositories
{
    internal class CourseInfo
    {
        public readonly OptionalDictionary<string, CourseModel> CourseCodeToCourse;
        public readonly OptionalDictionary<string, CourseModel> CourseNameToCourse;
        public readonly ISet<string> CourseCodes;
        public readonly ReadWriteLock Lock;
        
        public static readonly CourseInfo Instance = new CourseInfo();
        
        private CourseInfo()
        {
            CourseCodeToCourse = new OptionalDictionary<string, CourseModel>();
            CourseNameToCourse = new OptionalDictionary<string, CourseModel>();
            CourseCodes = new HashSet<string>();
            Lock = new ReadWriteLock();
        }
    }

    public class CourseRepository : ICourseRepository
    {
        private readonly CourseInfo _info;
        private readonly ISQExplorerContext _context;

        private void _addCourse(CourseModel course)
        {
            _info.CourseCodeToCourse[course.CourseCode] = course;
            _info.CourseNameToCourse[course.Name] = course;
            _info.CourseCodes.Add(course.CourseCode);
        }

        public CourseRepository(ISQExplorerContext context)
        {
            _context = context;
            _info = CourseInfo.Instance;

            _info.Lock.Write(() =>
            {
                if (_info.CourseCodes.None())
                {
                    _context.Courses.ForEach(_addCourse);
                }  
            });
        }

        public Task AddAsync(CourseModel course) => _info.Lock.Write(() =>
        {
            if (_info.CourseCodes.Contains(course.CourseCode))
            {
                return Task.CompletedTask;
            }

            _addCourse(course);
            _context.Courses.Add(course);
            return Task.CompletedTask;
        });

        public Task AddRangeAsync(IEnumerable<CourseModel> courses) => _info.Lock.Write(() =>
        {
            var c = courses.Where(co => !_info.CourseCodes.Contains(co.CourseCode)).ToList();
            c.ForEach(_addCourse);
            _context.Courses.AddRange(c);
            return Task.CompletedTask;
        });

        public async Task<Optional<CourseModel>> FromCourseCodeAsync(string courseCode) =>
            await _info.Lock.Read(() => Task.FromResult(_info.CourseCodeToCourse[courseCode]));

        public async Task<Optional<CourseModel>> FromCourseNameAsync(string courseName) =>
            await _info.Lock.Read(() => Task.FromResult(_info.CourseNameToCourse[courseName]));

        public IEnumerable<CourseModel> Courses => _info.Lock.Read(() => _info.CourseCodeToCourse.Values.Values().ToList());

        public Task SaveChangesAsync() => _context.SaveChangesAsync();

        public IEnumerator<CourseModel> GetEnumerator() => Courses.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Courses.GetEnumerator();
    }
}