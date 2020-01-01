using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using ISQExplorer.Functional;
using ISQExplorer.Misc;
using ISQExplorer.Models;
using ISQExplorer.Web;
using NUnit.Framework;

namespace ISQExplorerTests
{
    public class DataScraperTests
    {
        [Test]
        public async Task TestScrapeDepartmentIds()
        {
            (await DataScraper.ScrapeDepartmentIds()).Match(
                val =>
                {
                    var ids = val.ToList();
                    Assert.Greater(ids.Count, 0);
                    Assert.AreEqual(ids.Count, ids.ToHashSet().Count);
                },
                ex => Assert.Fail(ex.Message)
            );
        }

        [Test]
        public async Task TestScrapeDepartmentProfessor()
        {
            var ids = await DataScraper.ScrapeDepartmentIds();
            if (!ids)
            {
                Assert.Fail(ids.Exception.Message);
            }

            var dept = new Try<DepartmentModel>(() => ids.Value.First(x => x.Name == "Computing"));
            if (!dept)
            {
                Assert.Fail(dept.Exception.Message);
            }

            var res = await DataScraper.ScrapeDepartmentProfessor(
                "https://banner.unf.edu/pls/nfpo/wksfwbs.p_instructor_isq_grade?pv_instructor=N00959246",
                dept.Value
            );

            Assert.True(res.HasValue);
            Assert.NotNull(res.Value.Department);
            Assert.AreNotEqual(0, res.Value.Id);
            Assert.NotNull(res.Value.FirstName);
            Assert.NotNull(res.Value.LastName);
            Assert.NotNull(res.Value.NNumber);
            Assert.True(res.Value.NNumber.Matches(@"N\d{8}"));
        }

        [Test]
        public async Task TestScrapeDepartment()
        {
            var res = await DataScraper.ScrapeDepartmentIds();
            if (!res)
            {
                Assert.Fail(res.Exception.Message);
            }

            var dept = new Try<DepartmentModel>(() => res.Value.First(x => x.Name == "Computing"));
            if (!dept)
            {
                Assert.Fail(dept.Exception.Message);
            }

            var courseCount = 0;
            var profCount = 0;

            var scrape = await DataScraper.ScrapeDepartment(
                dept.Value,
                null,
                course =>
                {
                    courseCount++;
                    Assert.NotNull(course);
                    Assert.NotNull(course.CourseCode);
                    Assert.NotNull(course.Name);
                    Assert.NotNull(course.Department);
                    Assert.AreEqual(dept.Value, course.Department);
                },
                professor =>
                {
                    profCount++;
                    Assert.NotNull(professor);
                    Assert.NotNull(professor.Department);
                    Assert.NotNull(professor.FirstName);
                    Assert.NotNull(professor.LastName);
                    Assert.NotNull(professor.NNumber);
                    Assert.AreEqual(dept.Value, professor.Department);
                }
            );

            scrape.Match(val => Assert.Fail(val.Message));

            Assert.Greater(courseCount, 0);
            Assert.Greater(profCount, 0);
        }

        [Test]
        public async Task TestScrapeDepartmentProfessorEntries()
        {
            var res = await DataScraper.ScrapeDepartmentIds();
            if (!res)
            {
                Assert.Fail(res.Exception.Message);
            }

            var dept = Try.Of(() => res.Value.First(x => x.Name == "Computing"));
            if (!dept)
            {
                Assert.Fail(dept.Exception.Message);
            }

            var profs = new ConcurrentDictionary<string, ProfessorModel>();
            var courses = new ConcurrentDictionary<string, CourseModel>();

            var res2 = await DataScraper.ScrapeDepartment(
                dept.Value,
                null,
                course => courses[course.CourseCode] = course,
                professor => profs[professor.LastName] = professor
            );

            if (res2)
            {
                Assert.Fail(res2.Value.Message);
            }

            var prof = Try.Of(() => profs["Reddivari"]);
            if (!prof)
            {
                Assert.Fail("No professor found with last name 'Reddivari'");
            }

            var qqq = courses.ToFunc();

            var res3 = await DataScraper.ScrapeDepartmentProfessorEntries(
                prof.Value,
                courses.ToFunc()
            );

            if (!res3)
            {
                Assert.Fail(res3.Exception.Message);
            }

            var entries = res3.Value.ToList();
            Assert.Greater(entries.Count, 0);
            entries.ForEach(x =>
            {
                Assert.AreNotEqual(0, x.Crn);
                Assert.Greater(x.Pct1 + x.Pct2 + x.Pct3 + x.Pct4 + x.Pct5 + x.PctNa, 0.95);
                Assert.Greater(
                    x.PctA + x.PctAMinus + x.PctBPlus + x.PctB + x.PctBMinus + x.PctCPlus + x.PctC + x.PctD + x.PctF +
                    x.PctWithdraw, 0.90);
                Assert.NotNull(x.Professor);
                Assert.AreNotEqual(0, x.Year);
                Assert.AreNotEqual(0, x.NEnrolled);
                Assert.AreNotEqual(0.0, x.MeanGpa);
            });
            Assert.True(entries.Any(x => x.NResponded > 0));
            Assert.True(entries.Any(x => x.Course != null));
        }

        [Test]
        public async Task TestScrapeDepartmentCourseEntries()
        {
            var res = await DataScraper.ScrapeDepartmentIds();
            if (!res)
            {
                Assert.Fail(res.Exception.Message);
            }

            var dept = Try.Of(() => res.Value.First(x => x.Name == "Computing"));
            if (!dept)
            {
                Assert.Fail(dept.Exception.Message);
            }

            var profs = new ConcurrentDictionary<string, ProfessorModel>();
            var courses = new ConcurrentDictionary<string, CourseModel>();

            var res2 = await DataScraper.ScrapeDepartment(
                dept.Value,
                null,
                course => courses[course.CourseCode] = course,
                professor => profs[professor.LastName] = professor
            );

            if (res2)
            {
                Assert.Fail(res2.Value.Message);
            }

            var crn = Try.Of(() => courses["COP3503"]);
            if (!crn)
            {
                Assert.Fail("No course found with code 'COP3503'");
            }

            var res3 = await DataScraper.ScrapeDepartmentCourseEntries(
                crn.Value,
                profs.ToFunc()
            );

            if (!res3)
            {
                Assert.Fail(res3.Exception.Message);
            }

            var entries = res3.Value.ToList();
            Assert.Greater(entries.Count, 0);
            entries.ForEach(x =>
            {
                Assert.AreNotEqual(0, x.Crn);
                Assert.Greater(x.Pct1 + x.Pct2 + x.Pct3 + x.Pct4 + x.Pct5 + x.PctNa, 0.95);
                Assert.Greater(
                    x.PctA + x.PctAMinus + x.PctBPlus + x.PctB + x.PctBMinus + x.PctCPlus + x.PctC + x.PctD + x.PctF +
                    x.PctWithdraw, 0.90);
                Assert.NotNull(x.Course);
                Assert.AreNotEqual(0, x.Year);
                Assert.AreNotEqual(0, x.NEnrolled);
                Assert.AreNotEqual(0.0, x.MeanGpa);
            });
            Assert.True(entries.Any(x => x.NResponded > 0));
            Assert.True(entries.Any(x => x.Professor != null));
        }
    }
}