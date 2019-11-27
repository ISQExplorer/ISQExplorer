using System.Linq;
using System.Threading.Tasks;
using ISQExplorer.Models;
using ISQExplorer.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace ISQExplorerTests
{
    public class WebTests
    {
        private ISQExplorerContext _ctx;
        private ILogger<QueryRepository> _logger;

        [SetUp]
        public void Setup()
        {
            var contextOptions =
                new DbContextOptionsBuilder<ISQExplorerContext>().UseInMemoryDatabase(databaseName: "Test");
            _ctx = new ISQExplorerContext(contextOptions.Options);

            var courses = new[]
            {
                new CourseModel("Work with computer"),
                new CourseModel("BIG DATA"),
            };

            var courseCodes = new[]
            {
                new CourseCodeModel(courses[0], "COP3503", null, null),
                new CourseCodeModel(courses[1], "COP4710", null, null),
                new CourseCodeModel(courses[1], "COP3991", Season.Fall, 2019),
            };

            var courseNames = new[]
            {
                new CourseNameModel(courses[0], "Intro to Jesus", null, null),
                new CourseNameModel(courses[0], "Computer Science 2", Season.Fall, 2018),
                new CourseNameModel(courses[1], "Dutta Modeling", null, null),
                new CourseNameModel(courses[1], "Intro to Databases", Season.Fall, 2019),
            };
            
            _ctx.Courses.AddRange(courses);
            _ctx.CourseCodes.AddRange(courseCodes);
            _ctx.CourseNames.AddRange(courseNames);
            _logger = LoggerFactory.Create(logging => { }).CreateLogger<QueryRepository>();
        }

        [Test]
        public async Task TestWebScrapeCourseCode()
        {
            QueryHelper qh = new QueryHelper(_ctx, _logger);
            var res = (await qh.WebScrapeCourseCode("COP3503")).ToList();
            var courseMap = _ctx.CourseCodes.GroupBy(x => x.Course).ToDictionary(x => x.Key, x => x.Select(x => x.CourseCode).ToHashSet());
            Assert.True(res.All(x => courseMap.ContainsKey(x.Course) || courseMap[x.Course].Contains("COP3503")));
            Assert.True(res.Any());
        }
    }
}