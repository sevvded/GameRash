using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameRash.Data;
using GameRash.Models;

namespace GameRash.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LibraryController : ControllerBase
    {
        private readonly GameRashDbContext _context;
        private readonly ILogger<LibraryController> _logger;

        public LibraryController(GameRashDbContext context, ILogger<LibraryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/library
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetLibraries()
        {
            try
            {
                var libraries = await _context.Libraries
                    .Include(l => l.User)
                    .Include(l => l.Game)
                    .Select(l => new
                    {
                        l.LibraryID,
                        l.UserID,
                        Username = l.User != null ? l.User.Username : null,
                        l.GameID,
                        GameTitle = l.Game != null ? l.Game.Title : null,
                        l.AddedDate
                    })
                    .ToListAsync();

                return Ok(libraries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting libraries");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/library/user/5
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetUserLibrary(int userId)
        {
            try
            {
                var userLibrary = await _context.Libraries
                    .Include(l => l.Game)
                    .Where(l => l.UserID == userId)
                    .Select(l => new
                    {
                        l.LibraryID,
                        l.GameID,
                        GameTitle = l.Game != null ? l.Game.Title : null,
                        GameDescription = l.Game != null ? l.Game.Description : null,
                        CoverImage = l.Game != null ? l.Game.CoverImage : null,
                        l.AddedDate
                    })
                    .ToListAsync();

                return Ok(userLibrary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting library for user {UserId}", userId);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/library
        [HttpPost]
        public async Task<ActionResult<Library>> AddToLibrary(Library library)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if game is already in user's library
                var existingLibrary = await _context.Libraries
                    .FirstOrDefaultAsync(l => l.UserID == library.UserID && l.GameID == library.GameID);
                
                if (existingLibrary != null)
                {
                    return BadRequest("Game is already in user's library");
                }

                // Check if user exists
                var user = await _context.Users.FindAsync(library.UserID);
                if (user == null)
                {
                    return BadRequest("User not found");
                }

                // Check if game exists
                var game = await _context.Games.FindAsync(library.GameID);
                if (game == null)
                {
                    return BadRequest("Game not found");
                }

                library.AddedDate = DateTime.UtcNow;
                _context.Libraries.Add(library);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetUserLibrary), new { userId = library.UserID }, library);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding game to library");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // DELETE: api/library/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveFromLibrary(int id)
        {
            try
            {
                var library = await _context.Libraries.FindAsync(id);
                if (library == null)
                {
                    return NotFound($"Library entry with ID {id} not found");
                }

                _context.Libraries.Remove(library);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing game from library with ID {LibraryId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // DELETE: api/library/user/5/game/3
        [HttpDelete("user/{userId}/game/{gameId}")]
        public async Task<IActionResult> RemoveGameFromUserLibrary(int userId, int gameId)
        {
            try
            {
                var library = await _context.Libraries
                    .FirstOrDefaultAsync(l => l.UserID == userId && l.GameID == gameId);
                
                if (library == null)
                {
                    return NotFound($"Game {gameId} not found in user {userId}'s library");
                }

                _context.Libraries.Remove(library);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing game {GameId} from user {UserId}'s library", gameId, userId);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/library/check/user/5/game/3
        [HttpGet("check/user/{userId}/game/{gameId}")]
        public async Task<ActionResult<object>> CheckGameInLibrary(int userId, int gameId)
        {
            try
            {
                var library = await _context.Libraries
                    .FirstOrDefaultAsync(l => l.UserID == userId && l.GameID == gameId);

                var result = new
                {
                    UserID = userId,
                    GameID = gameId,
                    IsInLibrary = library != null,
                    AddedDate = library?.AddedDate
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if game {GameId} is in user {UserId}'s library", gameId, userId);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
