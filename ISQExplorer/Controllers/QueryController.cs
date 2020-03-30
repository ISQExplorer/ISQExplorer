#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using ISQExplorer.Functional;
using ISQExplorer.Misc;
using ISQExplorer.Models;
using ISQExplorer.Repositories;
using Microsoft.AspNetCore.Mvc;
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

        public QueryController(IQueryRepository repo, ITermRepository termRepo, ILogger<QueryController> logger)
        {
            _repo = repo;
            _logger = logger;
            _terms = termRepo;
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

            return Json(res);
        }

        [Route("Entries")]
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

            return Json(await _repo.QueryEntriesAsync(parameter, qt.Value, termSince, termUntil));
        }


        /*
        public async Task<IActionResult> RenderTableISQEntries(string parameter, QueryType qt,
            Season? sinceSeason = null, int? sinceYear = null,
            Season? untilSeason = null, int? untilYear = null, ISQEntriesOrderBy orderBy = ISQEntriesOrderBy.Time,
            bool descending = true)
        {
            var query = await _repo.QueryClass(parameter, qt, Term.FromNullable(sinceSeason, sinceYear),
                Term.FromNullable(untilSeason, untilYear));

            var res = descending
                ? orderBy switch
                {
                    ISQEntriesOrderBy.Time => query.OrderByDescending(x => x.Year).ThenByDescending(x => x.Season),
                    ISQEntriesOrderBy.LastName => query.OrderByDescending(x => x.Professor.LastName),
                    ISQEntriesOrderBy.Gpa => query.OrderByDescending(x => x.MeanGpa),
                    ISQEntriesOrderBy.Rating => query.OrderByDescending(x =>
                        x.Pct5 * 5 + x.Pct4 * 4 + x.Pct3 * 3 + x.Pct2 * 2 + x.Pct1 * 1),
                    _ => throw new ArgumentException($"Invalid orderBy value '{orderBy}'.")
                }
                : orderBy switch
                {
                    ISQEntriesOrderBy.Time => query.OrderBy(x => x.Year).ThenBy(x => x.Season),
                    ISQEntriesOrderBy.LastName => query.OrderBy(x => x.Professor.LastName),
                    ISQEntriesOrderBy.Gpa => query.OrderBy(x => x.MeanGpa),
                    ISQEntriesOrderBy.Rating => query.OrderBy(x =>
                        x.Pct5 * 5 + x.Pct4 * 4 + x.Pct3 * 3 + x.Pct2 * 2 + x.Pct1 * 1),
                    _ => throw new ArgumentException($"Invalid orderBy value '{orderBy}'.")
                };

            return View("RenderTableRows", res.AsEnumerable().Select(mod =>
            {
                var avgRating = mod.Pct5 * 0.05 + mod.Pct4 * 0.04 + mod.Pct3 * 0.03 + mod.Pct2 * 0.02 + mod.Pct1 * 0.01;
                return new object[]
                {
                    new Term(mod.Season, mod.Year),
                    mod.Crn,
                    mod.Course?.CourseCode,
                    mod.Professor?.LastName,
                    (100.0 * mod.NResponded / mod.NEnrolled).ToString("#.00"),
                    new TableCell(avgRating.ToString("#.00"), style: RatingToStyle(avgRating)),
                    new TableCell(mod.MeanGpa.ToString("#.00"), style: GpaToStyle(avgRating)),
                }.Select(x => new TableCell(x));
            }));
        }

        public IActionResult QueryEntries()
        {
            return View();
        }
        */
    }
}