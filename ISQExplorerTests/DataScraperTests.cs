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

            (await DataScraper.ScrapeDepartment(
                dept.Value,
                null,
                course =>
                {
                    Assert.NotNull(course);
                    Assert.AreEqual(dept, course.Department);
                },
                professor =>
                {
                    Assert.NotNull(professor);
                    Assert.AreEqual(dept, professor.Department);
                }
            )).Match(val => Assert.Fail(val.Message));
        }
    }
}