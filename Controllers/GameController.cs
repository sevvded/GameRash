using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameRash.Data;
using GameRash.Models;

namespace GameRash.Controllers
{
    public class GameController : Controller
    {
        private readonly GameRashDbContext _context;
        private readonly ILogger<GameController> _logger;

        public GameController(GameRashDbContext context, ILogger<GameController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Game/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var game = await _context.Games
                    .Include(g => g.Developer)
                    .Include(g => g.GameReviews)
                        .ThenInclude(gr => gr.User)
                    .FirstOrDefaultAsync(g => g.GameID == id);

                if (game == null)
                {
                    return NotFound($"Game with ID {id} not found");
                }

                var userId = HttpContext.Session.GetString("UserId");
                bool userOwnsGame = false;
                bool userHasReviewed = false;

                if (!string.IsNullOrEmpty(userId))
                {
                    userOwnsGame = await _context.Libraries
                        .AnyAsync(l => l.UserID == int.Parse(userId) && l.GameID == id);

                    userHasReviewed = await _context.GameReviews
                        .AnyAsync(gr => gr.UserID == int.Parse(userId) && gr.GameID == id);
                }

                ViewBag.UserOwnsGame = userOwnsGame;
                ViewBag.UserHasReviewed = userHasReviewed;
                ViewBag.UserId = userId;

                return View(game);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting game details for ID {GameId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: /Game/AddReview
        [HttpPost]
        public async Task<IActionResult> AddReview(int gameId, int rating, string reviewText = "")
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Giriş yapmanız gerekiyor" });
            }

            try
            {
                // Check if user owns the game
                var userOwnsGame = await _context.Libraries
                    .AnyAsync(l => l.UserID == int.Parse(userId) && l.GameID == gameId);

                if (!userOwnsGame)
                {
                    return Json(new { success = false, message = "Sadece sahip olduğunuz oyunlara yorum yapabilirsiniz" });
                }

                // Check if user already reviewed this game
                var existingReview = await _context.GameReviews
                    .FirstOrDefaultAsync(gr => gr.UserID == int.Parse(userId) && gr.GameID == gameId);

                if (existingReview != null)
                {
                    return Json(new { success = false, message = "Bu oyunu zaten değerlendirdiniz" });
                }

                var review = new GameReview
                {
                    UserID = int.Parse(userId),
                    GameID = gameId,
                    Rating = rating
                };

                _context.GameReviews.Add(review);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Yorumunuz eklendi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding review");
                return Json(new { success = false, message = "Bir hata oluştu" });
            }
        }

        // GET: /Game/Purchase/5
        public async Task<IActionResult> Purchase(int id)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                var game = await _context.Games
                    .Include(g => g.Developer)
                    .FirstOrDefaultAsync(g => g.GameID == id);

                if (game == null)
                {
                    return NotFound("Oyun bulunamadı");
                }

                // Check if user already owns this game
                var userOwnsGame = await _context.Libraries
                    .AnyAsync(l => l.UserID == int.Parse(userId) && l.GameID == id);

                if (userOwnsGame)
                {
                    TempData["ErrorMessage"] = "Bu oyun zaten kütüphanenizde bulunuyor";
                    return RedirectToAction("Details", new { id = id });
                }

                return View(game);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading purchase page for game {GameId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: /Game/CompletePurchase
        [HttpPost]
        public async Task<IActionResult> CompletePurchase(int gameId, string paymentMethod)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Giriş yapmanız gerekiyor" });
            }

            try
            {
                // Get game details
                var game = await _context.Games.FindAsync(gameId);
                if (game == null)
                {
                    return Json(new { success = false, message = "Oyun bulunamadı" });
                }

                // Check if user already owns this game
                var userOwnsGame = await _context.Libraries
                    .AnyAsync(l => l.UserID == int.Parse(userId) && l.GameID == gameId);

                if (userOwnsGame)
                {
                    return Json(new { success = false, message = "Bu oyun zaten kütüphanenizde bulunuyor" });
                }

                // Create purchase record
                var purchase = new Purchase
                {
                    UserID = int.Parse(userId),
                    GameID = gameId,
                    PurchaseDate = DateTime.UtcNow
                };

                _context.Purchases.Add(purchase);
                await _context.SaveChangesAsync();

                // Calculate total amount with tax
                var totalAmount = game.Price * 1.18m; // Adding 18% tax

                // Create payment record
                var payment = new Payment
                {
                    PurchaseID = purchase.PurchaseID,
                    PaymentMethod = paymentMethod,
                    Amount = totalAmount,
                    PaymentDate = DateTime.UtcNow,
                    Status = "Completed"
                };

                _context.Payments.Add(payment);

                // Add game to user's library
                var libraryEntry = new Library
                {
                    UserID = int.Parse(userId),
                    GameID = gameId,
                    AddedDate = DateTime.UtcNow
                };

                _context.Libraries.Add(libraryEntry);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Satın alma işlemi başarılı! Oyun kütüphanenize eklendi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing purchase for game {GameId}", gameId);
                return Json(new { success = false, message = "Satın alma işlemi sırasında bir hata oluştu" });
            }
        }
    }
}
