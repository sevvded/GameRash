using System.Diagnostics;
using GameRash.Models;
using GameRash.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameRash.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly GameRashDbContext _context;

        public HomeController(ILogger<HomeController> logger, GameRashDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var games = await _context.Games
                .Include(g => g.Developer)
                .Include(g => g.GameReviews)
                .Select(g => new
                {
                    g.GameID,
                    g.Title,
                    g.Description,
                    g.CoverImage,
                    g.Price,
                    DeveloperName = g.Developer != null ? g.Developer.StudioName : "Unknown",
                    AverageRating = g.GameReviews.Any() ? g.GameReviews.Average(gr => gr.Rating) : 0,
                    ReviewCount = g.GameReviews.Count
                })
                .ToListAsync();

            return View(games);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
