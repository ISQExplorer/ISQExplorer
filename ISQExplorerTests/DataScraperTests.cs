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
                null
            );

            scrape.Match(val => { }, ex => Assert.Fail(ex.Message));

            var (courses, professors, exceptions) = scrape.Value;

            courses.ForEach(course =>
            {
                courseCount++;
                Assert.NotNull(course);
                Assert.NotNull(course.CourseCode);
                Assert.NotNull(course.Name);
                Assert.NotNull(course.Department);
                Assert.AreEqual(dept.Value, course.Department);
            });

            professors.ForEach(professor =>
            {
                profCount++;
                Assert.NotNull(professor);
                Assert.NotNull(professor.Department);
                Assert.NotNull(professor.FirstName);
                Assert.NotNull(professor.LastName);
                Assert.NotNull(professor.NNumber);
                Assert.AreEqual(dept.Value, professor.Department);
            });

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

            var res2 = await DataScraper.ScrapeDepartment(dept.Value);

            res2.Match(val => { }, ex => Assert.Fail(res2.Exception.Message));

            var (coursesRes, professorsRes, errorsRes) = res2.Value;
            coursesRes.ForEach(course => courses[course.CourseCode] = course);
            professorsRes.ForEach(professor => profs[professor.LastName] = professor);

            var prof = Try.Of(() => profs["Reddivari"]);
            if (!prof)
            {
                Assert.Fail("No professor found with last name 'Reddivari'");
            }

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
            entries.ForEach(val =>
            {
                if (!val)
                {
                    return;
                }

                var x = val.Value;
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
            Assert.True(entries.Any(x => x && x.Value.NResponded > 0));
            Assert.True(entries.Any(x => x && x.Value.Course != null));
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

            var res2 = await DataScraper.ScrapeDepartment(dept.Value);

            res2.Match(val => { }, ex => Assert.Fail(res2.Exception.Message));

            var (coursesRes, professorsRes, errorsRes) = res2.Value;
            coursesRes.ForEach(course => courses[course.CourseCode] = course);
            professorsRes.ForEach(professor => profs[professor.LastName] = professor);

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

        [Test]
        public async Task TestScrapeAll()
        {
            var res = await DataScraper.ScrapeAll();
            if (!res)
            {
                Assert.Fail(res.Exception.Message);
            }

            var result = res.Value;
            var courses = result.Courses.Succeeded.ToList();
            var professors = result.Professors.Succeeded.ToList();
            var entries = result.Entries.ToList();

            Assert.Greater(courses.Count, 0);
            Assert.Greater(professors.Count, 0);
            Assert.Greater(entries.Count, 0);

            courses.ForEach(x =>
            {
                Assert.NotNull(x.Department, x.ToString());
                Assert.NotNull(x.CourseCode, x.ToString());
                Assert.NotNull(x.Name, x.ToString());
            });

            entries.ForEach(x =>
            {
                Assert.AreNotEqual(0, x.Crn, x.ToString());
                Assert.AreNotEqual(0, x.Year, x.ToString());
            });

            Assert.True(entries.AtLeastPercent(0.85, x => x.NEnrolled > 0));
            Assert.True(entries.AtLeastPercent(0.85, x => x.Pct1 + x.Pct2 + x.Pct3 + x.Pct4 + x.Pct5 + x.PctNa > 0.95),
                ">15% of entries have rating scales that do not add up to at least 95%.");
            Assert.True(entries.AtLeastPercent(0.85, x =>
                x.PctA + x.PctAMinus + x.PctBPlus + x.PctB + x.PctBMinus + x.PctCPlus + x.PctC + x.PctD + x.PctF +
                x.PctWithdraw > 0.95), ">15% of entries have grade scales that do not add up to at least 95%.");
            Assert.True(entries.AtLeastPercent(0.85, x => x.Course != null), ">15% of entry courses are null.");
            Assert.True(entries.AtLeastPercent(0.85, x => x.MeanGpa > 0.05), ">15% of GPAs are 0.0.");

            professors.ForEach(x =>
            {
                Assert.NotNull(x.Department, x.ToString());
                Assert.NotNull(x.FirstName, x.ToString());
                Assert.NotNull(x.LastName, x.ToString());
                Assert.NotNull(x.NNumber, x.ToString());
            });
        }
    }
}