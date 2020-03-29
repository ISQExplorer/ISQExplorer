#nullable enable
using System.Linq;
using System.Threading.Tasks;
using ISQExplorer.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ISQExplorer.Controllers
{
    public class TableCell
    {
        public readonly string Contents;
        public readonly string? Class;
        public readonly string? Style;

        public TableCell(object contents, string? @class = null, string? style = null)
        {
            if (contents is TableCell tc)
            {
                (Contents, Class, Style) = (tc.Contents, $"${tc.Class} {@class ?? ""}".Trim(),
                    $"{tc.Style} {@style ?? ""}".Trim());
            }
            else
            {
                (Contents, Class, Style) = (contents?.ToString() ?? "null", @class?.Trim(), style?.Trim());
            }
        }

        public TableCell(TableCell other)
        {
            (Contents, Class, Style) = (other.Contents, other.Class, other.Style);
        }

        public override string ToString()
        {
            var cl = Class != null ? $" class='{Class}'" : "";
            var st = Style != null ? $" style='{Style}'" : "";
            return $"<td{cl}{st}>{Contents}</td>";
        }
    }

    [ApiController]
    [Route("[controller]")]
    public class QueryController : Controller
    {
        private readonly IQueryRepository _repo;
        private readonly ILogger<QueryController> _logger;

        public QueryController(IQueryRepository repo, ILogger<QueryController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<IActionResult> GetSuggestions(string parameter, int? count = null)
        {
            var suggestions = await Task.WhenAll(
                _repo.QueryEntriesAsync(parameter, QueryType.CourseCode),
                _repo.QueryEntriesAsync(parameter, QueryType.CourseName),
                _repo.QueryEntriesAsync(parameter, QueryType.ProfessorName)
            );

            if (count == null)
            {
                count = -1;
            }

            var courseCodes = (count >= 0 ? suggestions[0].Take(count.Value) : suggestions[0]).AsEnumerable();
            var courseNames = (count >= 0 ? suggestions[1].Take(count.Value) : suggestions[1]).AsEnumerable();
            var profNames = (count >= 0 ? suggestions[2].Take(count.Value) : suggestions[2]).AsEnumerable();

            return Json(
                courseCodes.Select(x => new Suggestion(QueryType.CourseCode, x.Course.CourseCode))
                .Concat(courseNames.Select(x => new Suggestion(QueryType.CourseName, x.Course.Name)))
                .Concat(profNames.Select(x =>
                    new Suggestion(QueryType.ProfessorName, $"{x.Professor.FirstName} {x.Professor.LastName}"))));
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