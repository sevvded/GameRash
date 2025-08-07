using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameRash.Data;
using GameRash.Models;

namespace GameRash.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DbTestController : ControllerBase
    {
        private readonly GameRashDbContext _context;

        public DbTestController(GameRashDbContext context)
        {
            _context = context;
        }

        [HttpGet("connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                // Veritabanı bağlantısını test et
                var canConnect = await _context.Database.CanConnectAsync();
                
                if (canConnect)
                {
                    return Ok(new { 
                        message = "Database connection successful", 
                        timestamp = DateTime.UtcNow 
                    });
                }
                else
                {
                    return BadRequest(new { 
                        message = "Database connection failed", 
                        timestamp = DateTime.UtcNow 
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Database connection error", 
                    error = ex.Message,
                    timestamp = DateTime.UtcNow 
                });
            }
        }

        [HttpGet("users-count")]
        public async Task<IActionResult> GetUsersCount()
        {
            try
            {
                var count = await _context.Users.CountAsync();
                return Ok(new { 
                    userCount = count, 
                    timestamp = DateTime.UtcNow 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Error getting users count", 
                    error = ex.Message,
                    timestamp = DateTime.UtcNow 
                });
            }
        }

        [HttpGet("games-count")]
        public async Task<IActionResult> GetGamesCount()
        {
            try
            {
                var count = await _context.Games.CountAsync();
                return Ok(new { 
                    gameCount = count, 
                    timestamp = DateTime.UtcNow 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Error getting games count", 
                    error = ex.Message,
                    timestamp = DateTime.UtcNow 
                });
            }
        }
    }
}
