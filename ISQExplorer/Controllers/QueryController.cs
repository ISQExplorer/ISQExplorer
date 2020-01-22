using System.Linq;
using System.Threading.Tasks;
using ISQExplorer.Models;
using ISQExplorer.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace ISQExplorer.Controllers
{
    public class QueryController : Controller
    {
        private readonly IQueryRepository _repo;

        public QueryController(IQueryRepository repo)
        {
            _repo = repo;
        }

        public async Task<IActionResult> QueryClass(string? courseCode = null, string? courseName = null,
            string? professorName = null, Season? sinceSeason = null, int? sinceYear = null,
            Season? untilSeason = null, int? untilYear = null)
        {
            var query = new QueryParams(courseCode, courseName, professorName)
            {
                Since = new Term(sinceSeason ?? Season.Spring, sinceYear ?? 0),
                Until = new Term(untilSeason ?? Season.Fall, untilYear ?? int.MaxValue)
            };
            var res = await _repo.QueryClass(query);
            return View(res.OrderByDescending(x => x.Year).ThenByDescending(x => x.Season));
        }

        public IActionResult QueryPage()
        {
            return View();
        }
    }
}