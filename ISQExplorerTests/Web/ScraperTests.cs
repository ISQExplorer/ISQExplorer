using System.Linq;
using System.Threading.Tasks;
using ISQExplorer.Repositories;
using ISQExplorer.Web;
using Moq;
using NUnit.Framework;

namespace ISQExplorerTests
{
    public class ScraperTests
    {
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
        public async Task TestDepartmentScrape()
        {
            var scraper = InitializeScraper(nameof(TestDepartmentScrape));
            await scraper.ScrapeDepartmentsAsync();
            
            Assert.True(scraper.Departments.Any());
        }
    }
}