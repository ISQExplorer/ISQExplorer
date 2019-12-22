using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ISQExplorer.Functional;
using ISQExplorer.Models;
using ISQExplorer.Repositories;
using ISQExplorer.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    }
}