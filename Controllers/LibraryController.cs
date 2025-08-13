using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameRash.Data;
using GameRash.Models;

namespace GameRash.Controllers
{
    public class AddToLibraryRequest
    {
        public int GameId { get; set; }
    }

    public class LibraryController : Controller
    {
        private readonly GameRashDbContext _context;
        private readonly ILogger<LibraryController> _logger;

        public LibraryController(GameRashDbContext context, ILogger<LibraryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var userLibrary = await _context.Libraries
                .Include(l => l.Game)
                .ThenInclude(g => g.Developer)
                .Where(l => l.UserID == int.Parse(userId))
                .Select(l => new
                {
                    l.Game.GameID,
                    l.Game.Title,
                    l.Game.Description,
                    l.Game.CoverImage,
                    l.Game.Price,
                    DeveloperName = l.Game.Developer != null ? l.Game.Developer.StudioName : "Unknown",
                    AddedDate = l.AddedDate
                })
                .ToListAsync();

            return View(userLibrary);
        }

        [HttpPost]
        public async Task<IActionResult> AddToLibrary([FromBody] AddToLibraryRequest request)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Giriş yapmanız gerekiyor" });
            }

            try
            {
                // Önce oyunun var olup olmadığını kontrol et
                var game = await _context.Games.FindAsync(request.GameId);
                if (game == null)
                {
                    return Json(new { success = false, message = "Oyun bulunamadı" });
                }

                var existingLibrary = await _context.Libraries
                    .FirstOrDefaultAsync(l => l.UserID == int.Parse(userId) && l.GameID == request.GameId);

                if (existingLibrary != null)
                {
                    return Json(new { success = false, message = "Bu oyun zaten kütüphanenizde" });
                }

                var library = new Library
                {
                    UserID = int.Parse(userId),
                    GameID = request.GameId,
                    AddedDate = DateTime.UtcNow
                };

                _context.Libraries.Add(library);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Oyun kütüphaneye eklendi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding game to library");
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromLibrary(int gameId)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Giriş yapmanız gerekiyor" });
            }

            try
            {
                var library = await _context.Libraries
                    .FirstOrDefaultAsync(l => l.UserID == int.Parse(userId) && l.GameID == gameId);

                if (library == null)
                {
                    return Json(new { success = false, message = "Oyun kütüphanenizde bulunamadı" });
                }

                _context.Libraries.Remove(library);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Oyun kütüphaneden kaldırıldı" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing game from library");
                return Json(new { success = false, message = "Bir hata oluştu" });
            }
        }
    }
}
