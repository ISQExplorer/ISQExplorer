using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ISQExplorer.Models;
using ISQExplorer.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

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
                (Contents, Class, Style) = (contents.ToString(), @class?.Trim(), style?.Trim());
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

    public class Suggestion
    {
        public readonly QueryType Type;
        public readonly string Parameter;

        public Suggestion(QueryType qt, string parameter)
        {
            (Type, Parameter) = (qt, parameter);
        }
    }

    public class QueryController : Controller
    {
        private readonly IQueryRepository _repo;

        public QueryController(IQueryRepository repo)
        {
            _repo = repo;
        }

        private string GpaToStyle(double gpa)
        {
            var hue = (int) (gpa / 4.00 * 100.0);
            return $"color: hsl({hue}, 100%, 35%)";
        }

        private string RatingToStyle(double rating)
        {
            var hue = (int) ((rating - 1.00) / 4.00 * 100.0);
            return $"color: hsl({hue}, 100%, 35%)";
        }

        [HttpPost]
        public IActionResult RenderTableRows([FromBody] IEnumerable<IEnumerable<TableCell>> data)
        {
            return View(data);
        }

        public enum ISQEntriesOrderBy
        {
            Time = 0,
            LastName = 1,
            Gpa = 2,
            Rating = 3
        }

        public async Task<IActionResult> GetSuggestions(string parameter, int count = 0)
        {
            var suggestions = await Task.WhenAll(
                _repo.QueryClass(parameter, QueryType.CourseCode),
                _repo.QueryClass(parameter, QueryType.CourseName),
                _repo.QueryClass(parameter, QueryType.ProfessorName)
            );

            var courseCodes = (count >= 0 ? suggestions[0].Take(count) : suggestions[0]).AsEnumerable();
            var courseNames = (count >= 0 ? suggestions[1].Take(count) : suggestions[1]).AsEnumerable();
            var profNames = (count >= 0 ? suggestions[2].Take(count) : suggestions[2]).AsEnumerable();

            return Json(
                courseCodes.Select(x => new Suggestion(QueryType.CourseCode, x.Course.CourseCode))
                .Concat(courseNames.Select(x => new Suggestion(QueryType.CourseName, x.Course.Name)))
                .Concat(profNames.Select(x =>
                    new Suggestion(QueryType.ProfessorName, $"{x.Professor.FirstName} {x.Professor.LastName}"))));
        }
        

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
    }
}