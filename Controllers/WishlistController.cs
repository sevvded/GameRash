using Microsoft.AspNetCore.Mvc;
using GameRash.Data;
using GameRash.Models;
using Microsoft.EntityFrameworkCore;

namespace GameRash.Controllers
{
    public class AddToWishlistRequest
    {
        public int GameId { get; set; }
    }

    public class WishlistController : Controller
    {
        private readonly GameRashDbContext _context;
        private readonly ILogger<WishlistController> _logger;

        public WishlistController(GameRashDbContext context, ILogger<WishlistController> logger)
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

            var userWishlist = await _context.Wishlists
                .Include(w => w.Game)
                .ThenInclude(g => g.Developer)
                .Where(w => w.UserID == int.Parse(userId))
                .Select(w => new
                {
                    w.Game.GameID,
                    w.Game.Title,
                    w.Game.Description,
                    w.Game.CoverImage,
                    w.Game.Price,
                    DeveloperName = w.Game.Developer != null ? w.Game.Developer.StudioName : "Unknown",
                    w.AddedDate
                })
                .ToListAsync();

            return View(userWishlist);
        }

        [HttpPost]
        public async Task<IActionResult> AddToWishlist([FromBody] AddToWishlistRequest request)
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

                var existingWishlist = await _context.Wishlists
                    .FirstOrDefaultAsync(w => w.UserID == int.Parse(userId) && w.GameID == request.GameId);

                if (existingWishlist != null)
                {
                    return Json(new { success = false, message = "Bu oyun zaten istek listenizde" });
                }

                var wishlist = new Wishlist
                {
                    UserID = int.Parse(userId),
                    GameID = request.GameId,
                    AddedDate = DateTime.UtcNow
                };

                _context.Wishlists.Add(wishlist);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Oyun istek listesine eklendi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding game to wishlist");
                return Json(new { success = false, message = "Bir hata oluştu" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromWishlist(int gameId)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Giriş yapmanız gerekiyor" });
            }

            try
            {
                var wishlist = await _context.Wishlists
                    .FirstOrDefaultAsync(w => w.UserID == int.Parse(userId) && w.GameID == gameId);

                if (wishlist == null)
                {
                    return Json(new { success = false, message = "Oyun istek listenizde bulunamadı" });
                }

                _context.Wishlists.Remove(wishlist);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Oyun istek listesinden kaldırıldı" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing game from wishlist");
                return Json(new { success = false, message = "Bir hata oluştu" });
            }
        }
    }
}
