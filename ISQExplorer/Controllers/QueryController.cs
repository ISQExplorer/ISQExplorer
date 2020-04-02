#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using ISQExplorer.Functional;
using ISQExplorer.Misc;
using ISQExplorer.Models;
using ISQExplorer.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ISQExplorer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class QueryController : Controller
    {
        private readonly IQueryRepository _repo;
        private readonly ILogger<QueryController> _logger;
        private readonly ITermRepository _terms;
        private readonly ISQExplorerContext _context;

        public QueryController(IQueryRepository repo, ITermRepository termRepo, ISQExplorerContext context,
            ILogger<QueryController> logger)
        {
            _repo = repo;
            _logger = logger;
            _terms = termRepo;
            _context = context;
        }

        [Route("QueryTypes")]
        public IActionResult GetQueryTypes()
        {
            var res = Enum.GetValues(typeof(QueryType)).Cast<QueryType>();
            return Json(res.Select(x => new {Name = x.AsString(), Value = x}));
        }

        [Route("Terms")]
        public IActionResult GetTerms()
        {
            return Json(_terms.AsEnumerable());
        }

        [Route("Suggestions/{Parameter}/{Count?}")]
        public async Task<IActionResult> GetSuggestions(string parameter, int? count = null)
        {
            var res = await _repo.QuerySuggestionsAsync(parameter,
                QueryType.CourseCode | QueryType.CourseName | QueryType.ProfessorName);

            if (count != null)
            {
                if (count.Value > 100)
                {
                    count = 100;
                }

                res = res.Take(count.Value);
            }

            return Ok(res);
        }

        [Route("Entries/{queryType}/{parameter}")]
        public async Task<IActionResult> GetEntries(string parameter, string queryType, int? since, int? until)
        {
            var qt = int.TryParse(queryType, out var num)
                ? EnumConversions.FromInt<QueryType>(num)
                : EnumConversions.FromString<QueryType>(queryType);

            if (!qt)
            {
                return BadRequest($"Invalid query type '{queryType}'.");
            }

            TermModel? termSince = null;
            if (since != null)
            {
                var tmp = await _terms.FromIdAsync(since.Value);
                if (!tmp)
                {
                    return BadRequest($"Invalid since term '{since}'.");
                }

                termSince = tmp.Value;
            }


            TermModel? termUntil = null;
            if (until != null)
            {
                var tmp = await _terms.FromIdAsync(until.Value);
                if (!tmp)
                {
                    return BadRequest($"Invalid since term '{since}'.");
                }

                termUntil = tmp.Value;
            }

            return Ok(await _repo.QueryEntriesAsync(parameter, qt.Value, termSince, termUntil));
        }

        [Route("Course/{courseCode}")]
        public IActionResult GetCourseFromCourseCode(string courseCode)
        {
            var res = _context.Courses.Include(x => x.Department)
                .SingleOrDefault(x => x.CourseCode == courseCode.ToUpper());
            if (res == null)
            {
                return NotFound();
            }

            return Ok(res);
        }

        [Route("Professor/{nNumber}")]
        public IActionResult GetProfessorFromNNumber(string nNumber)
        {
            var res = _context.Professors.Include(x => x.Department)
                .SingleOrDefault(x => x.NNumber == nNumber.ToUpper());
            if (res == null)
            {
                return NotFound();
            }

            return Ok(res);
        }
    }
}