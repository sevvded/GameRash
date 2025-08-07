using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameRash.Data;
using GameRash.Models;

namespace GameRash.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WishlistController : ControllerBase
    {
        private readonly GameRashDbContext _context;
        private readonly ILogger<WishlistController> _logger;

        public WishlistController(GameRashDbContext context, ILogger<WishlistController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/wishlist
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetWishlists()
        {
            try
            {
                var wishlists = await _context.Wishlists
                    .Include(w => w.User)
                    .Include(w => w.Game)
                    .Select(w => new
                    {
                        w.WishlistID,
                        w.UserID,
                        Username = w.User != null ? w.User.Username : null,
                        w.GameID,
                        GameTitle = w.Game != null ? w.Game.Title : null,
                        w.AddedDate
                    })
                    .ToListAsync();

                return Ok(wishlists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting wishlists");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/wishlist/user/5
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetUserWishlist(int userId)
        {
            try
            {
                var userWishlist = await _context.Wishlists
                    .Include(w => w.Game)
                    .Where(w => w.UserID == userId)
                    .Select(w => new
                    {
                        w.WishlistID,
                        w.GameID,
                        GameTitle = w.Game != null ? w.Game.Title : null,
                        GameDescription = w.Game != null ? w.Game.Description : null,
                        CoverImage = w.Game != null ? w.Game.CoverImage : null,
                        w.AddedDate
                    })
                    .ToListAsync();

                return Ok(userWishlist);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting wishlist for user {UserId}", userId);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/wishlist
        [HttpPost]
        public async Task<ActionResult<Wishlist>> AddToWishlist(Wishlist wishlist)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if game is already in user's wishlist
                var existingWishlist = await _context.Wishlists
                    .FirstOrDefaultAsync(w => w.UserID == wishlist.UserID && w.GameID == wishlist.GameID);
                
                if (existingWishlist != null)
                {
                    return BadRequest("Game is already in user's wishlist");
                }

                // Check if user exists
                var user = await _context.Users.FindAsync(wishlist.UserID);
                if (user == null)
                {
                    return BadRequest("User not found");
                }

                // Check if game exists
                var game = await _context.Games.FindAsync(wishlist.GameID);
                if (game == null)
                {
                    return BadRequest("Game not found");
                }

                wishlist.AddedDate = DateTime.UtcNow;
                _context.Wishlists.Add(wishlist);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetUserWishlist), new { userId = wishlist.UserID }, wishlist);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding game to wishlist");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // DELETE: api/wishlist/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveFromWishlist(int id)
        {
            try
            {
                var wishlist = await _context.Wishlists.FindAsync(id);
                if (wishlist == null)
                {
                    return NotFound($"Wishlist entry with ID {id} not found");
                }

                _context.Wishlists.Remove(wishlist);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing game from wishlist with ID {WishlistId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // DELETE: api/wishlist/user/5/game/3
        [HttpDelete("user/{userId}/game/{gameId}")]
        public async Task<IActionResult> RemoveGameFromUserWishlist(int userId, int gameId)
        {
            try
            {
                var wishlist = await _context.Wishlists
                    .FirstOrDefaultAsync(w => w.UserID == userId && w.GameID == gameId);
                
                if (wishlist == null)
                {
                    return NotFound($"Game {gameId} not found in user {userId}'s wishlist");
                }

                _context.Wishlists.Remove(wishlist);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing game {GameId} from user {UserId}'s wishlist", gameId, userId);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/wishlist/check/user/5/game/3
        [HttpGet("check/user/{userId}/game/{gameId}")]
        public async Task<ActionResult<object>> CheckGameInWishlist(int userId, int gameId)
        {
            try
            {
                var wishlist = await _context.Wishlists
                    .FirstOrDefaultAsync(w => w.UserID == userId && w.GameID == gameId);

                var result = new
                {
                    UserID = userId,
                    GameID = gameId,
                    IsInWishlist = wishlist != null,
                    AddedDate = wishlist?.AddedDate
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if game {GameId} is in user {UserId}'s wishlist", gameId, userId);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/wishlist/move-to-library/user/5/game/3
        [HttpPost("move-to-library/user/{userId}/game/{gameId}")]
        public async Task<ActionResult<object>> MoveFromWishlistToLibrary(int userId, int gameId)
        {
            try
            {
                // Check if game is in wishlist
                var wishlist = await _context.Wishlists
                    .FirstOrDefaultAsync(w => w.UserID == userId && w.GameID == gameId);
                
                if (wishlist == null)
                {
                    return BadRequest("Game is not in user's wishlist");
                }

                // Check if game is already in library
                var existingLibrary = await _context.Libraries
                    .FirstOrDefaultAsync(l => l.UserID == userId && l.GameID == gameId);
                
                if (existingLibrary != null)
                {
                    return BadRequest("Game is already in user's library");
                }

                // Add to library
                var library = new Library
                {
                    UserID = userId,
                    GameID = gameId,
                    AddedDate = DateTime.UtcNow
                };

                _context.Libraries.Add(library);

                // Remove from wishlist
                _context.Wishlists.Remove(wishlist);

                await _context.SaveChangesAsync();

                var result = new
                {
                    Message = "Game moved from wishlist to library successfully",
                    UserID = userId,
                    GameID = gameId,
                    LibraryID = library.LibraryID
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving game {GameId} from wishlist to library for user {UserId}", gameId, userId);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
