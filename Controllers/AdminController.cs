using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameRash.Data;
using GameRash.Models;

namespace GameRash.Controllers
{
    public class AdminController : Controller
    {
        private readonly GameRashDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(GameRashDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Check if user is admin
        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("IsAdmin") == "true";
        }

        // Check if user is developer
        private bool IsDeveloper()
        {
            return HttpContext.Session.GetString("IsDeveloper") == "true";
        }

        // Check if user has any management access
        private bool IsAuthorized()
        {
            return IsAdmin() || IsDeveloper();
        }

        // Get developer ID for current user (if developer)
        private int? GetCurrentDeveloperId()
        {
            var developerIdStr = HttpContext.Session.GetString("DeveloperId");
            return int.TryParse(developerIdStr, out int developerId) ? developerId : null;
        }

        // GET: /admin
        public async Task<IActionResult> Index()
        {
            if (!IsAuthorized())
            {
                return RedirectToAction("Login", "Auth");
            }

            // Show different dashboard based on role
            if (IsAdmin())
            {
                var adminStats = await GetAdminStatistics();
                return View("AdminDashboard", adminStats);
            }
            else
            {
                var developerStats = await GetDeveloperStatistics();
                return View("DeveloperDashboard", developerStats);
            }
        }

        // GET: /admin/games
        public async Task<IActionResult> Games()
        {
            if (!IsAuthorized())
            {
                return RedirectToAction("Login", "Auth");
            }

            IQueryable<Game> gamesQuery = _context.Games
                .Include(g => g.Developer)
                .Include(g => g.GameReviews);

            // If developer, only show their games
            if (IsDeveloper() && !IsAdmin())
            {
                var developerId = GetCurrentDeveloperId();
                if (developerId.HasValue)
                {
                    gamesQuery = gamesQuery.Where(g => g.DeveloperID == developerId.Value);
                }
            }

            var games = await gamesQuery
                .Select(g => new
                {
                    g.GameID,
                    g.Title,
                    g.Description,
                    g.CoverImage,
                    g.Price,
                    g.DeveloperID,
                    DeveloperName = g.Developer != null ? g.Developer.StudioName : "Unknown",
                    ReviewCount = g.GameReviews.Count,
                    AverageRating = g.GameReviews.Any() ? g.GameReviews.Average(r => r.Rating) : 0,
                    CanEdit = true // We'll handle this in the view
                })
                .ToListAsync();

            ViewBag.IsAdmin = IsAdmin();
            ViewBag.IsDeveloper = IsDeveloper();
            ViewBag.CurrentDeveloperId = GetCurrentDeveloperId();

            return View(games);
        }

        // GET: /admin/addgame
        public async Task<IActionResult> AddGame()
        {
            if (!IsAuthorized())
            {
                return RedirectToAction("Login", "Auth");
            }

            if (IsAdmin())
            {
                // Admins can assign any developer
                ViewBag.Developers = await _context.Developers.ToListAsync();
            }
            else if (IsDeveloper())
            {
                // Developers can only create games for their own studio
                var developerId = GetCurrentDeveloperId();
                ViewBag.Developers = await _context.Developers
                    .Where(d => d.DeveloperID == developerId)
                    .ToListAsync();
            }

            ViewBag.IsAdmin = IsAdmin();
            ViewBag.CurrentDeveloperId = GetCurrentDeveloperId();

            return View();
        }

        // POST: /admin/addgame
        [HttpPost]
        public async Task<IActionResult> AddGame(Game game, IFormFile? coverImage)
        {
            if (!IsAuthorized())
            {
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                // If developer, force their developer ID
                if (IsDeveloper() && !IsAdmin())
                {
                    var developerId = GetCurrentDeveloperId();
                    if (!developerId.HasValue)
                    {
                        TempData["ErrorMessage"] = "Developer bilgisi bulunamadı.";
                        return View(game);
                    }
                    game.DeveloperID = developerId.Value;
                }

                // Validate that the developer exists and user has permission
                var developer = await _context.Developers.FindAsync(game.DeveloperID);
                if (developer == null)
                {
                    TempData["ErrorMessage"] = "Geçersiz developer seçimi.";
                    await PopulateViewBag();
                    return View(game);
                }

                // Check permission for non-admin users
                if (!IsAdmin() && developer.DeveloperID != GetCurrentDeveloperId())
                {
                    TempData["ErrorMessage"] = "Bu stüdyo için oyun ekleyemezsiniz.";
                    await PopulateViewBag();
                    return View(game);
                }

                // Handle cover image upload
                if (coverImage != null && coverImage.Length > 0)
                {
                    var fileName = Path.GetFileNameWithoutExtension(coverImage.FileName);
                    var extension = Path.GetExtension(coverImage.FileName);
                    var uniqueFileName = $"{fileName}_{Guid.NewGuid()}{extension}";

                    var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                    if (!Directory.Exists(uploadsDir))
                    {
                        Directory.CreateDirectory(uploadsDir);
                    }

                    var filePath = Path.Combine(uploadsDir, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await coverImage.CopyToAsync(fileStream);
                    }

                    game.CoverImage = uniqueFileName;
                }

                _context.Games.Add(game);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Oyun başarıyla eklendi!";
                return RedirectToAction("Games");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding game");
                TempData["ErrorMessage"] = "Oyun eklenirken bir hata oluştu.";
                await PopulateViewBag();
                return View(game);
            }
        }

        // GET: /admin/editgame/5
        public async Task<IActionResult> EditGame(int id)
        {
            if (!IsAuthorized())
            {
                return RedirectToAction("Login", "Auth");
            }

            var game = await _context.Games
                .Include(g => g.Developer)
                .FirstOrDefaultAsync(g => g.GameID == id);

            if (game == null)
            {
                return NotFound();
            }

            // Check permission for non-admin users
            if (!IsAdmin() && game.DeveloperID != GetCurrentDeveloperId())
            {
                TempData["ErrorMessage"] = "Bu oyunu düzenleme yetkiniz yok.";
                return RedirectToAction("Games");
            }

            await PopulateViewBag();
            return View(game);
        }

        // POST: /admin/editgame/5
        [HttpPost]
        public async Task<IActionResult> EditGame(int id, Game game, IFormFile? coverImage)
        {
            if (!IsAuthorized())
            {
                return RedirectToAction("Login", "Auth");
            }

            if (id != game.GameID)
            {
                return BadRequest();
            }

            try
            {
                var existingGame = await _context.Games.FindAsync(id);
                if (existingGame == null)
                {
                    return NotFound();
                }

                // Check permission for non-admin users
                if (!IsAdmin() && existingGame.DeveloperID != GetCurrentDeveloperId())
                {
                    TempData["ErrorMessage"] = "Bu oyunu düzenleme yetkiniz yok.";
                    return RedirectToAction("Games");
                }

                // If developer, don't allow changing developer ID
                if (IsDeveloper() && !IsAdmin())
                {
                    game.DeveloperID = existingGame.DeveloperID;
                }

                // Handle cover image upload
                if (coverImage != null && coverImage.Length > 0)
                {
                    var fileName = Path.GetFileNameWithoutExtension(coverImage.FileName);
                    var extension = Path.GetExtension(coverImage.FileName);
                    var uniqueFileName = $"{fileName}_{Guid.NewGuid()}{extension}";

                    var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                    if (!Directory.Exists(uploadsDir))
                    {
                        Directory.CreateDirectory(uploadsDir);
                    }

                    var filePath = Path.Combine(uploadsDir, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await coverImage.CopyToAsync(fileStream);
                    }

                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(existingGame.CoverImage))
                    {
                        var oldImagePath = Path.Combine(uploadsDir, existingGame.CoverImage);
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    existingGame.CoverImage = uniqueFileName;
                }

                // Update game properties
                existingGame.Title = game.Title;
                existingGame.Description = game.Description;
                existingGame.Price = game.Price;
                existingGame.DeveloperID = game.DeveloperID;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Oyun başarıyla güncellendi!";
                return RedirectToAction("Games");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating game");
                TempData["ErrorMessage"] = "Oyun güncellenirken bir hata oluştu.";
                await PopulateViewBag();
                return View(game);
            }
        }

        // POST: /admin/deletegame/5
        [HttpPost]
        public async Task<IActionResult> DeleteGame(int id)
        {
            if (!IsAuthorized())
            {
                return Json(new { success = false, message = "Yetkisiz erişim" });
            }

            try
            {
                var game = await _context.Games.FindAsync(id);
                if (game == null)
                {
                    return Json(new { success = false, message = "Oyun bulunamadı" });
                }

                // Check permission for non-admin users
                if (!IsAdmin() && game.DeveloperID != GetCurrentDeveloperId())
                {
                    return Json(new { success = false, message = "Bu oyunu silme yetkiniz yok" });
                }

                // Delete cover image if exists
                if (!string.IsNullOrEmpty(game.CoverImage))
                {
                    var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", game.CoverImage);
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _context.Games.Remove(game);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Oyun başarıyla silindi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting game");
                return Json(new { success = false, message = "Oyun silinirken bir hata oluştu" });
            }
        }

        // GET: /admin/users (Admin only)
        public async Task<IActionResult> Users()
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bu sayfaya erişim yetkiniz yok.";
                return RedirectToAction("Index");
            }

            var users = await _context.Users
                .Include(u => u.Admin)
                .Include(u => u.Developer)
                .Select(u => new
                {
                    u.UserID,
                    u.Username,
                    u.Email,
                    IsAdmin = u.Admin != null,
                    IsDeveloper = u.Developer != null,
                    StudioName = u.Developer != null ? u.Developer.StudioName : null
                })
                .ToListAsync();

            return View(users);
        }

        private async Task PopulateViewBag()
        {
            if (IsAdmin())
            {
                ViewBag.Developers = await _context.Developers.ToListAsync();
            }
            else if (IsDeveloper())
            {
                var developerId = GetCurrentDeveloperId();
                ViewBag.Developers = await _context.Developers
                    .Where(d => d.DeveloperID == developerId)
                    .ToListAsync();
            }

            ViewBag.IsAdmin = IsAdmin();
            ViewBag.CurrentDeveloperId = GetCurrentDeveloperId();
        }

        private async Task<object> GetAdminStatistics()
        {
            return new
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalGames = await _context.Games.CountAsync(),
                TotalDevelopers = await _context.Developers.CountAsync(),
                TotalAdmins = await _context.Admins.CountAsync()
            };
        }

        private async Task<object> GetDeveloperStatistics()
        {
            var developerId = GetCurrentDeveloperId();
            if (!developerId.HasValue) return new { };

            var gamesCount = await _context.Games
                .Where(g => g.DeveloperID == developerId.Value)
                .CountAsync();

            var totalSales = await _context.Purchases
                .Include(p => p.Game)
                .Where(p => p.Game.DeveloperID == developerId.Value)
                .CountAsync();

            var averageRating = await _context.GameReviews
                .Include(r => r.Game)
                .Where(r => r.Game.DeveloperID == developerId.Value)
                .AverageAsync(r => (double?)r.Rating) ?? 0;

            return new
            {
                GamesCount = gamesCount,
                TotalSales = totalSales,
                AverageRating = Math.Round(averageRating, 1)
            };
        }
    }
}