using System.Linq;
using System.Threading.Tasks;
using ISQExplorer.Models;
using ISQExplorer.Repositories;
using ISQExplorer.Web;
using Moq;
using NUnit.Framework;

namespace ISQExplorerTests
{
    public class ScraperTests
    {
        public DepartmentModel ComputingDepartment { get; set; } = new DepartmentModel
        {
            Id = 6502,
            Name = "Computing"
        };

        public TermModel Fall2019 { get; set; } = new TermModel
        {
            Id = 201980,
            Name = "Fall 2019"
        };

        public static Scraper InitializeScraper(string testName)
        {
            var context = Mock.DbContext(testName);
            var termRepo = new TermRepository(context);
            var deptRepo = new DepartmentRepository(context);
            var profRepo = new ProfessorRepository(context);
            var entryRepo = new EntryRepository(context);
            var courseRepo = new CourseRepository(context);
            var htmlClient = new HtmlClient(() => new RateLimiter(3, 1000));

            return new Scraper(termRepo, profRepo, deptRepo, entryRepo, courseRepo, htmlClient);
        }

        [Test]
        public async Task TestScrapeDepartments()
        {
            var scraper = InitializeScraper(nameof(TestScrapeDepartments));
            var res = await scraper.ScrapeDepartmentsAsync();

            Assert.False(res.IsError);

            var depts = scraper.Departments.ToList();
            Assert.True(depts.Any());
        }

        [Test]
        public async Task TestScrapeTerms()
        {
            var scraper = InitializeScraper(nameof(TestScrapeTerms));
            var res = await scraper.ScrapeTermsAsync();

            Assert.False(res.IsError);

            var terms = scraper.Terms.ToList();
            Assert.True(terms.Any());
        }

        [Test]
        public async Task TestScrapeCourses()
        {
            var scraper = InitializeScraper(nameof(TestScrapeCourses));
            var res = await scraper.ScrapeCoursesAsync(ComputingDepartment, Fall2019);

            Assert.False(res.IsError);

            var courses = scraper.Courses.ToList();
            Assert.True(courses.Any());
        }

        [Test]
        public async Task TestScrapeProfessors()
        {
            var scraper = InitializeScraper(nameof(TestScrapeProfessors));
            var res = await scraper.ScrapeProfessorsAsync(ComputingDepartment, Fall2019);

            Assert.False(res.IsError);

            var professors = scraper.Professors.ToList();
            Assert.True(professors.Any());
        }
    }
}