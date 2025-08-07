using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameRash.Data;
using GameRash.Models;

namespace GameRash.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PurchaseController : ControllerBase
    {
        private readonly GameRashDbContext _context;
        private readonly ILogger<PurchaseController> _logger;

        public PurchaseController(GameRashDbContext context, ILogger<PurchaseController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/purchase
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Purchase>>> GetPurchases()
        {
            try
            {
                var purchases = await _context.Purchases
                    .Include(p => p.User)
                    .Include(p => p.Game)
                    .Include(p => p.Payments)
                    .ToListAsync();

                return Ok(purchases);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting purchases");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/purchase/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Purchase>> GetPurchase(int id)
        {
            try
            {
                var purchase = await _context.Purchases
                    .Include(p => p.User)
                    .Include(p => p.Game)
                    .Include(p => p.Payments)
                    .FirstOrDefaultAsync(p => p.PurchaseID == id);

                if (purchase == null)
                {
                    return NotFound($"Purchase with ID {id} not found");
                }

                return Ok(purchase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting purchase with ID {PurchaseId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/purchase/user/5
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<Purchase>>> GetPurchasesByUser(int userId)
        {
            try
            {
                var purchases = await _context.Purchases
                    .Include(p => p.Game)
                    .Include(p => p.Payments)
                    .Where(p => p.UserID == userId)
                    .ToListAsync();

                return Ok(purchases);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting purchases for user {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/purchase/game/5
        [HttpGet("game/{gameId}")]
        public async Task<ActionResult<IEnumerable<Purchase>>> GetPurchasesByGame(int gameId)
        {
            try
            {
                var purchases = await _context.Purchases
                    .Include(p => p.User)
                    .Include(p => p.Payments)
                    .Where(p => p.GameID == gameId)
                    .ToListAsync();

                return Ok(purchases);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting purchases for game {GameId}", gameId);
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/purchase
        [HttpPost]
        public async Task<ActionResult<Purchase>> CreatePurchase(Purchase purchase)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if user exists
                var user = await _context.Users.FindAsync(purchase.UserID);
                if (user == null)
                {
                    return BadRequest("User not found");
                }

                // Check if game exists
                var game = await _context.Games.FindAsync(purchase.GameID);
                if (game == null)
                {
                    return BadRequest("Game not found");
                }

                // Check if user already owns this game
                var existingPurchase = await _context.Purchases
                    .FirstOrDefaultAsync(p => p.UserID == purchase.UserID && p.GameID == purchase.GameID);
                
                if (existingPurchase != null)
                {
                    return BadRequest("User already owns this game");
                }

                purchase.PurchaseDate = DateTime.UtcNow;
                _context.Purchases.Add(purchase);
                await _context.SaveChangesAsync();

                // Add game to user's library
                var libraryEntry = new Library
                {
                    UserID = purchase.UserID,
                    GameID = purchase.GameID,
                    AddedDate = DateTime.UtcNow
                };

                _context.Libraries.Add(libraryEntry);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetPurchase), new { id = purchase.PurchaseID }, purchase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating purchase");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/purchase/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePurchase(int id, Purchase purchase)
        {
            try
            {
                if (id != purchase.PurchaseID)
                {
                    return BadRequest("Purchase ID mismatch");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var existingPurchase = await _context.Purchases.FindAsync(id);
                if (existingPurchase == null)
                {
                    return NotFound($"Purchase with ID {id} not found");
                }

                // Only allow updating certain fields
                existingPurchase.PurchaseDate = purchase.PurchaseDate;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating purchase with ID {PurchaseId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/purchase/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePurchase(int id)
        {
            try
            {
                var purchase = await _context.Purchases.FindAsync(id);
                if (purchase == null)
                {
                    return NotFound($"Purchase with ID {id} not found");
                }

                _context.Purchases.Remove(purchase);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting purchase with ID {PurchaseId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/purchase/statistics
        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetPurchaseStatistics()
        {
            try
            {
                var totalPurchases = await _context.Purchases.CountAsync();
                var totalRevenue = await _context.Purchases
                    .Include(p => p.Payments)
                    .SumAsync(p => p.Payments.Sum(pay => pay.Amount));

                var purchasesByMonth = await _context.Purchases
                    .GroupBy(p => new { p.PurchaseDate.Year, p.PurchaseDate.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToListAsync();

                var topGames = await _context.Purchases
                    .GroupBy(p => p.GameID)
                    .Select(g => new
                    {
                        GameID = g.Key,
                        PurchaseCount = g.Count()
                    })
                    .OrderByDescending(x => x.PurchaseCount)
                    .Take(10)
                    .ToListAsync();

                return Ok(new
                {
                    TotalPurchases = totalPurchases,
                    TotalRevenue = totalRevenue,
                    PurchasesByMonth = purchasesByMonth,
                    TopGames = topGames
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting purchase statistics");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
