using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ISQExplorer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;

namespace ISQExplorer.Controllers
{
    public class WebScrapeController : Controller
    {
        private readonly ISQExplorerContext _context;

        public WebScrapeController(ISQExplorerContext context)
        {
            _context = context;
        }

        public IActionResult QueryClass(string courseCode = null, string nNumber = null, string name = null)
        {
            var chain = _context.IsqEntries.Select(x => x);

            if (courseCode != null)
            {
                chain = chain.Where(x => x.CourseCode.StartsWith(courseCode));
            }

            if (nNumber != null)
            {
                chain = chain.Where(x => x.Professor.NNumber.StartsWith(nNumber));
            }

            if (name != null)
            {
                if (!name.Contains(' '))
                {
                    chain = chain.Where(x => x.Professor.LastName.StartsWith(name));
                }
                else
                {
                    var spl = name.Split();
                    var lname = spl.Last();
                    var fname = string.Join(" ", spl.SkipLast(1));
                    chain = chain.Where(x => x.Professor.LastName.StartsWith(lname) && x.Professor.FirstName.StartsWith(fname));
                }
            }

            return View(chain.ToList());
        }
    }
}