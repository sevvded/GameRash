using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameRash.Data;
using GameRash.Models;

namespace GameRash.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly GameRashDbContext _context;
        private readonly ILogger<UserController> _logger;

        public UserController(GameRashDbContext context, ILogger<UserController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/user
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetUsers()
        {
            try
            {
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
                        DeveloperStudio = u.Developer != null ? u.Developer.StudioName : null
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/user/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetUser(int id)
        {
            try
            {
                // Önce temel kullanıcı bilgilerini alalım
                var user = await _context.Users
                    .Where(u => u.UserID == id)
                    .Select(u => new
                    {
                        u.UserID,
                        u.Username,
                        u.Email
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound($"User with ID {id} not found");
                }

                // Admin bilgilerini ayrı sorgu ile alalım
                var adminInfo = await _context.Admins
                    .Where(a => a.UserID == id)
                    .Select(a => new { a.AdminID })
                    .FirstOrDefaultAsync();

                // Developer bilgilerini ayrı sorgu ile alalım
                var developerInfo = await _context.Developers
                    .Where(d => d.UserID == id)
                    .Select(d => new { d.DeveloperID, d.StudioName, d.Bio })
                    .FirstOrDefaultAsync();

                // Kütüphane bilgilerini ayrı sorgu ile alalım
                var libraries = await _context.Libraries
                    .Include(l => l.Game)
                    .Where(l => l.UserID == id)
                    .Select(l => new
                    {
                        l.LibraryID,
                        l.GameID,
                        GameTitle = l.Game != null ? l.Game.Title : null,
                        l.AddedDate
                    })
                    .ToListAsync();

                // İstek listesi bilgilerini ayrı sorgu ile alalım
                var wishlists = await _context.Wishlists
                    .Include(w => w.Game)
                    .Where(w => w.UserID == id)
                    .Select(w => new
                    {
                        w.WishlistID,
                        w.GameID,
                        GameTitle = w.Game != null ? w.Game.Title : null,
                        w.AddedDate
                    })
                    .ToListAsync();

                // Satın alma bilgilerini ayrı sorgu ile alalım
                var purchases = await _context.Purchases
                    .Include(p => p.Game)
                    .Where(p => p.UserID == id)
                    .Select(p => new
                    {
                        p.PurchaseID,
                        p.GameID,
                        GameTitle = p.Game != null ? p.Game.Title : null,
                        p.PurchaseDate
                    })
                    .ToListAsync();

                // Yorum bilgilerini ayrı sorgu ile alalım
                var reviews = await _context.GameReviews
                    .Include(gr => gr.Game)
                    .Where(gr => gr.UserID == id)
                    .Select(gr => new
                    {
                        gr.ReviewID,
                        gr.GameID,
                        GameTitle = gr.Game != null ? gr.Game.Title : null,
                        gr.Rating
                    })
                    .ToListAsync();

                // Tüm bilgileri birleştirip döndür
                var result = new
                {
                    user.UserID,
                    user.Username,
                    user.Email,
                    IsAdmin = adminInfo != null,
                    IsDeveloper = developerInfo != null,
                    DeveloperStudio = developerInfo?.StudioName,
                    DeveloperBio = developerInfo?.Bio,
                    Libraries = libraries,
                    Wishlists = wishlists,
                    Purchases = purchases,
                    GameReviews = reviews
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user with ID {UserId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/user/username/admin_user
        [HttpGet("username/{username}")]
        public async Task<ActionResult<User>> GetUserByUsername(string username)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Admin)
                    .Include(u => u.Developer)
                    .FirstOrDefaultAsync(u => u.Username == username);

                if (user == null)
                {
                    return NotFound($"User with username {username} not found");
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user with username {Username}", username);
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/user
        [HttpPost]
        public async Task<ActionResult<User>> CreateUser(User user)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if username already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == user.Username);
                
                if (existingUser != null)
                {
                    return BadRequest("Username already exists");
                }

                // Check if email already exists
                existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == user.Email);
                
                if (existingUser != null)
                {
                    return BadRequest("Email already exists");
                }

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetUser), new { id = user.UserID }, user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/user/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, User user)
        {
            try
            {
                if (id != user.UserID)
                {
                    return BadRequest("User ID mismatch");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var existingUser = await _context.Users.FindAsync(id);
                if (existingUser == null)
                {
                    return NotFound($"User with ID {id} not found");
                }

                // Check if username is being changed and if it already exists
                if (existingUser.Username != user.Username)
                {
                    var usernameExists = await _context.Users
                        .AnyAsync(u => u.Username == user.Username && u.UserID != id);
                    
                    if (usernameExists)
                    {
                        return BadRequest("Username already exists");
                    }
                }

                // Check if email is being changed and if it already exists
                if (existingUser.Email != user.Email)
                {
                    var emailExists = await _context.Users
                        .AnyAsync(u => u.Email == user.Email && u.UserID != id);
                    
                    if (emailExists)
                    {
                        return BadRequest("Email already exists");
                    }
                }

                existingUser.Username = user.Username;
                existingUser.Password = user.Password;
                existingUser.Email = user.Email;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/user/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound($"User with ID {id} not found");
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with ID {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/user/5/library
        [HttpGet("{id}/library")]
        public async Task<ActionResult<IEnumerable<Library>>> GetUserLibrary(int id)
        {
            try
            {
                var library = await _context.Libraries
                    .Include(l => l.Game)
                    .Where(l => l.UserID == id)
                    .ToListAsync();

                return Ok(library);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting library for user {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/user/5/wishlist
        [HttpGet("{id}/wishlist")]
        public async Task<ActionResult<IEnumerable<Wishlist>>> GetUserWishlist(int id)
        {
            try
            {
                var wishlist = await _context.Wishlists
                    .Include(w => w.Game)
                    .Where(w => w.UserID == id)
                    .ToListAsync();

                return Ok(wishlist);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting wishlist for user {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
