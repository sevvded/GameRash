using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameRash.Data;
using GameRash.Models;

namespace GameRash.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        private readonly GameRashDbContext _context;
        private readonly ILogger<GameController> _logger;

        public GameController(GameRashDbContext context, ILogger<GameController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/game
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetGames()
        {
            try
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
                        g.DeveloperID,
                        DeveloperName = g.Developer != null ? g.Developer.StudioName : null,
                        AverageRating = g.GameReviews.Any() ? g.GameReviews.Average(gr => gr.Rating) : 0,
                        ReviewCount = g.GameReviews.Count
                    })
                    .ToListAsync();
                
                return Ok(games);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting games");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/game/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetGame(int id)
        {
            try
            {
                // Temel oyun bilgilerini alalım
                var game = await _context.Games
                    .Where(g => g.GameID == id)
                    .Select(g => new
                    {
                        g.GameID,
                        g.Title,
                        g.Description,
                        g.CoverImage,
                        g.DeveloperID
                    })
                    .FirstOrDefaultAsync();

                if (game == null)
                {
                    return NotFound($"Game with ID {id} not found");
                }

                // Developer bilgilerini ayrı sorgu ile alalım
                var developerInfo = await _context.Developers
                    .Where(d => d.DeveloperID == game.DeveloperID)
                    .Select(d => new { d.StudioName, d.Bio })
                    .FirstOrDefaultAsync();

                // Yorum bilgilerini ayrı sorgu ile alalım
                var reviews = await _context.GameReviews
                    .Include(gr => gr.User)
                    .Where(gr => gr.GameID == id)
                    .Select(gr => new
                    {
                        gr.ReviewID,
                        gr.UserID,
                        Username = gr.User != null ? gr.User.Username : null,
                        gr.Rating
                    })
                    .ToListAsync();

                // Satın alma sayısını alalım
                var purchaseCount = await _context.Purchases
                    .Where(p => p.GameID == id)
                    .CountAsync();

                // Kütüphaneye eklenme sayısını alalım
                var libraryCount = await _context.Libraries
                    .Where(l => l.GameID == id)
                    .CountAsync();

                // İstek listesine eklenme sayısını alalım
                var wishlistCount = await _context.Wishlists
                    .Where(w => w.GameID == id)
                    .CountAsync();

                // Tüm bilgileri birleştirip döndür
                var result = new
                {
                    game.GameID,
                    game.Title,
                    game.Description,
                    game.CoverImage,
                    game.DeveloperID,
                    DeveloperStudio = developerInfo?.StudioName,
                    DeveloperBio = developerInfo?.Bio,
                    AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0,
                    ReviewCount = reviews.Count,
                    PurchaseCount = purchaseCount,
                    LibraryCount = libraryCount,
                    WishlistCount = wishlistCount,
                    Reviews = reviews
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting game with ID {GameId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/game
        [HttpPost]
        public async Task<ActionResult<Game>> CreateGame(Game game)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _context.Games.Add(game);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetGame), new { id = game.GameID }, game);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating game");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // PUT: api/game/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateGame(int id, Game game)
        {
            try
            {
                if (id != game.GameID)
                {
                    return BadRequest("Game ID mismatch");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var existingGame = await _context.Games.FindAsync(id);
                if (existingGame == null)
                {
                    return NotFound($"Game with ID {id} not found");
                }

                existingGame.Title = game.Title;
                existingGame.Description = game.Description;
                existingGame.CoverImage = game.CoverImage;
                existingGame.DeveloperID = game.DeveloperID;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating game with ID {GameId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // DELETE: api/game/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGame(int id)
        {
            try
            {
                var game = await _context.Games.FindAsync(id);
                if (game == null)
                {
                    return NotFound($"Game with ID {id} not found");
                }

                _context.Games.Remove(game);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting game with ID {GameId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
