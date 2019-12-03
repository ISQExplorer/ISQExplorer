using System;
using System.Collections;
using System.Collections.Generic;
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
        private Dictionary<CourseModel, HashSet<string>> _courseMap;
        private int _ctr = 0;

        [SetUp]
        public void Setup()
        {
            var contextOptions =
                new DbContextOptionsBuilder<ISQExplorerContext>().UseInMemoryDatabase("Test" + _ctr++)
                    .EnableSensitiveDataLogging();
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

            var professors = new[]
            {
                new ProfessorModel("N00959246", "Sandeep", "Reddivari"),
                new ProfessorModel("N00823146", "Asai", "Asaithambi"),
            };

            _ctx.Courses.AddRange(courses);
            _ctx.CourseCodes.AddRange(courseCodes);
            _ctx.CourseNames.AddRange(courseNames);
            _ctx.Professors.AddRange(professors);
            _ctx.SaveChanges();
            _logger = LoggerFactory.Create(logging => { }).CreateLogger<QueryRepository>();
            _courseMap = _ctx.CourseCodes.AsEnumerable().GroupBy(x => x.Course)
                .ToDictionary(x => x.Key, x => x.Select(y => y.CourseCode).ToHashSet());
        }

        [Test]
        public async Task TestWebScrapeCourseCode()
        {
            QueryHelper qh = new QueryHelper(_ctx, _logger);
            var res = (await qh.WebScrapeCourseCode("COP3503")).ToList();
            Assert.True(res.All(x => _courseMap.ContainsKey(x.Course) || _courseMap[x.Course].Contains("COP3503")));
            Assert.True(res.Any(x => x.Professor != null && x.Professor.LastName.Equals("Reddivari")));
        }

        [Test]
        public async Task TestWebScrapeNNumber()
        {
            QueryHelper qh = new QueryHelper(_ctx, _logger);
            var res = (await qh.WebScrapeNNumber("N00959246")).ToList();
            Assert.True(res.All(x => x.Professor.LastName.Equals("Reddivari")));
            Assert.True(res.Any(x =>
                x.Course != null && _courseMap.ContainsKey(x.Course) && _courseMap[x.Course].Contains("COP3503")));
        }

        [Test]
        public async Task TestNameToProfessors()
        {
            var qr = new QueryRepository(_ctx, _logger);
            var res1 = (await qr.NameToProfessors("Asaithambi")).ToList();
            var res2 = (await qr.NameToProfessors("Sandeep Reddivari")).ToList();
            var res3 = (await qr.NameToProfessors("Yeetus")).ToList();
            var res4 = (await qr.NameToProfessors("Yeetus Reddivari")).ToList();
            Assert.AreEqual(1, res1.Count);
            Assert.AreEqual("Asai", res1.First().FirstName);
            Assert.AreEqual(1, res2.Count);
            Assert.AreEqual("Sandeep", res2.First().FirstName);
            Assert.AreEqual(0, res3.Count);
            Assert.AreEqual(0, res4.Count);
        }

        private static object ConditionToObject(Action<List<ISQEntryModel>, List<ISQEntryModel>> a)
        {
            return a;
        }

        private static object[] _queryCases =
        {
            new[]
            {
                new QueryParams {CourseCode = "COP3503"},
                ConditionToObject(CollectionAssert.AreEquivalent)
            },
            new[]
            {
                new QueryParams {ProfessorName = "Reddivari"},
                ConditionToObject(CollectionAssert.AreEquivalent)
            }
        };

        [Test, TestCaseSource(nameof(_queryCases))]
        public async Task TestQueryClass(QueryParams qp, Action<List<ISQEntryModel>, List<ISQEntryModel>> assertion)
        {
            // make sure we start out with no entries as a sanity check
            var qr = new QueryRepository(_ctx, _logger);
            Assert.AreEqual(0, _ctx.IsqEntries.Count());
            Assert.AreEqual(0, _ctx.Queries.Count());

            // this should do a web query because this query is not cached yet
            var res1 = (await qr.QueryClass(qp)).ToList();
            // make sure we get at least one result
            Assert.Greater(_ctx.IsqEntries.Count(), 0);

            // make sure the query registered with our cache
            var queries = _ctx.Queries.ToList();
            Assert.AreEqual(1, queries.Count);
            Assert.True(qp == queries.First());

            // this should be a sql query because this query is cached
            var res2 = (await qr.QueryClass(qp)).ToList();
            Assert.Greater(res1.Count, 0);
            CollectionAssert.AreEquivalent(res1, res2);

            // make sure the query was not registered twice
            queries = _ctx.Queries.ToList();
            Assert.AreEqual(1, queries.Count);
            Assert.True(qp == queries.First());

            // run the given assertion function on the two lists
            assertion(res1, res2);
        }
    }
}