using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISQExplorer.Functional;
using ISQExplorer.Misc;
using ISQExplorer.Models;
using ISQExplorer.Repositories;
using ISQExplorer.Web;
using Microsoft.EntityFrameworkCore.Internal;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace ISQExplorerTests
{
    public class ScraperTests
    {
        public static DepartmentModel ComputingDepartment { get; set; } = new DepartmentModel
        {
            Id = 6502,
            Name = "Computing"
        };

        public static TermModel Fall2019 { get; set; } = new TermModel
        {
            Id = 201980,
            Name = "Fall 2019"
        };

        public static ProfessorModel Sandy { get; set; } = new ProfessorModel
        {
            FirstName = "Sandeep",
            LastName = "Reddivari",
            NNumber = "N00959246",
            Department = ComputingDepartment
        };

        public class ScraperEnv
        {
            private Mock<ICourseRepository>? _courses;
            private Mock<IDepartmentRepository>? _depts;
            private Mock<IEntryRepository>? _entries;
            private Mock<IProfessorRepository>? _professors;
            private Mock<ITermRepository>? _terms;
            private IHtmlClient _htmlClient;

            public ScraperEnv(IHtmlClient? htmlClient = null)
            {
                _htmlClient = htmlClient ?? new HtmlClient();
            }

            public ScraperEnv SaveCourses(out IList<CourseModel> courses)
            {
                _courses = Fake.CourseRepository(out courses);
                return this;
            }

            public ScraperEnv SaveDepartments(out IList<DepartmentModel> departments)
            {
                _depts = Fake.DepartmentRepository(out departments);
                return this;
            }

            public ScraperEnv SaveEntries(out IList<ISQEntryModel> entries)
            {
                _entries = Fake.EntryRepository(out entries);
                return this;
            }

            public ScraperEnv SaveProfessors(out IList<ProfessorModel> professors)
            {
                _professors = Fake.ProfessorRepository(out professors);
                return this;
            }

            public ScraperEnv SaveTerms(out IList<TermModel> terms)
            {
                _terms = Fake.TermRepository(out terms);
                return this;
            }

            public Scraper Scraper()
            {
                return new Scraper((_terms ?? Fake.TermRepository(out _)).Object,
                    (_professors ?? Fake.ProfessorRepository(out _)).Object,
                    (_depts ?? Fake.DepartmentRepository(out _)).Object,
                    (_entries ?? Fake.EntryRepository(out _)).Object,
                    (_courses ?? Fake.CourseRepository(out _)).Object,
                    _htmlClient);
            }
        }

        [SetUp]
        public void SetUp()
        {
            var baseDirectory = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.Parent?.Parent?.Parent
                ?.FullName;
            Directory.SetCurrentDirectory(baseDirectory);
        }

        [Test]
        public async Task TestScrapeDepartments()
        {
            var htmlClient = new Fake.FakeHtmlClient()
                .OnGet(Urls.ProfessorPage(Sandy.NNumber),
                    File.ReadAllText("Web/Pages/DepartmentSchedule.html"))
                .DefaultToException();

            var scraper = new ScraperEnv(htmlClient)
                .SaveDepartments(out var depts)
                .Scraper();
            var res = await scraper.ScrapeDepartmentsAsync();

            Assert.False(res.IsError);

            Assert.True(depts.Any());
            Assert.True(scraper.Errors.None());
        }

        [Test]
        public async Task TestScrapeTerms()
        {
            var htmlClient = new Fake.FakeHtmlClient()
                .OnGet(Urls.ProfessorPage(Sandy.NNumber),
                    File.ReadAllText("Web/Pages/DepartmentSchedule.html"))
                .DefaultToException();

            var scraper = new ScraperEnv(htmlClient)
                .SaveTerms(out var terms)
                .Scraper();
            var res = await scraper.ScrapeTermsAsync();

            Assert.False(res.IsError);

            Assert.True(terms.Any());
            Assert.True(scraper.Errors.None());
        }

        [Test]
        public async Task TestScrapeCourses()
        {
            var htmlClient = new Fake.FakeHtmlClient()
                .OnGet(Urls.ProfessorPage(Sandy.NNumber),
                    File.ReadAllText("Web/Pages/ComputingFall2019.html"))
                .DefaultToException();

            var scraper = new ScraperEnv(htmlClient)
                .SaveCourses(out var courses)
                .Scraper();
            var res = await scraper.ScrapeCoursesAsync(ComputingDepartment, Fall2019);

            Assert.False(res.IsError);

            Assert.True(courses.Distinct().Count() == 51);
            Assert.True(scraper.Errors.None());
        }

        [Test]
        public async Task TestScrapeProfessors()
        {
            var scraper = new ScraperEnv()
                .SaveProfessors(out var professors)
                .Scraper();
            var res = await scraper.ScrapeProfessorsAsync(ComputingDepartment, Fall2019);

            Assert.False(res.IsError);

            Assert.True(professors.Any());
            Assert.True(scraper.Errors.None());
        }

        [Test]
        public async Task TestScrapeProfessorEntries()
        {
            var htmlClient = new Fake.FakeHtmlClient()
                .OnGet(Urls.ProfessorPage(Sandy.NNumber),
                    File.ReadAllText("Web/Pages/TestScrapeProfessorEntriesPage.html"))
                .DefaultToException();

            var scraper = new ScraperEnv(htmlClient)
                .SaveEntries(out var entries)
                .Scraper();
            var res = await scraper.ScrapeProfessorEntriesAsync(Sandy);

            Assert.False(res.IsError);

            Assert.True(entries.Count == 30);
            Assert.True(entries.All(x => x.Course != null));
            Assert.True(entries.All(x => x.Professor != null));
            Assert.True(entries.All(x => x.Term != null));
        }
    }

    public class FullScraperTests
    {
        private Fake.FakeHtmlClient _client;
        private int _countOrig = 0;
        private const string HtmlJsonPath = "Web/Pages/HtmlClientCache.json.gz";

        [SetUp]
        public async Task Setup()
        {
            var baseDirectory = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.Parent?.Parent?.Parent
                ?.FullName;
            Directory.SetCurrentDirectory(baseDirectory);

            if (File.Exists(HtmlJsonPath))
            {
                var bytes = File.ReadAllBytes(HtmlJsonPath);
                var str = await Fake.Compressor.Decompress(bytes);
                _client = await Fake.FakeHtmlClient.Deserialize(str);
            }
            else
            {
                _client = new Fake.FakeHtmlClient();
            }

            _countOrig = _client.Count;
            _client.DefaultToWeb()
                .SetAfterGetCallback(s => System.Diagnostics.Trace.WriteLine($"GET {s}"))
                .SetAfterPostDictCallback((s, d) => System.Diagnostics.Trace.WriteLine($"POST {s}:{JsonConvert.SerializeObject(d)}"))
                .SetAfterPostStringCallback((s, p) => System.Diagnostics.Trace.WriteLine($"POST {s}:'{p}''"));
        }

        [TearDown]
        public async Task TearDown()
        {
            var str = _client.Serialize();
            var compressed = await Fake.Compressor.Compress(str);
            
            File.WriteAllBytes(HtmlJsonPath, compressed);
        }

        [Test]
        public async Task TestScraper()
        {
            var ctx = Fake.DbContext();
            var scraper = new Scraper(new TermRepository(ctx), new ProfessorRepository(ctx),
                new DepartmentRepository(ctx), new EntryRepository(ctx), new CourseRepository(ctx), _client);

            var res = await scraper.ScrapeEntriesAsync();
            
            Assert.False(res.IsError);
            Assert.True(scraper.Entries.Any());
            Assert.True(scraper.Errors.None());
        }
    }
}