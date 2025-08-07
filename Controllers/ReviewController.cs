using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameRash.Data;
using GameRash.Models;

namespace GameRash.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewController : ControllerBase
    {
        private readonly GameRashDbContext _context;
        private readonly ILogger<ReviewController> _logger;

        public ReviewController(GameRashDbContext context, ILogger<ReviewController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/review
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetReviews()
        {
            try
            {
                var reviews = await _context.GameReviews
                    .Include(gr => gr.User)
                    .Include(gr => gr.Game)
                    .Select(gr => new
                    {
                        gr.ReviewID,
                        gr.UserID,
                        Username = gr.User != null ? gr.User.Username : null,
                        gr.GameID,
                        GameTitle = gr.Game != null ? gr.Game.Title : null,
                        gr.Rating
                    })
                    .ToListAsync();

                return Ok(reviews);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reviews");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/review/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetReview(int id)
        {
            try
            {
                var review = await _context.GameReviews
                    .Include(gr => gr.User)
                    .Include(gr => gr.Game)
                    .Where(gr => gr.ReviewID == id)
                    .Select(gr => new
                    {
                        gr.ReviewID,
                        gr.UserID,
                        Username = gr.User != null ? gr.User.Username : null,
                        gr.GameID,
                        GameTitle = gr.Game != null ? gr.Game.Title : null,
                        gr.Rating
                    })
                    .FirstOrDefaultAsync();

                if (review == null)
                {
                    return NotFound($"Review with ID {id} not found");
                }

                return Ok(review);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting review with ID {ReviewId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/review/game/5
        [HttpGet("game/{gameId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetReviewsByGame(int gameId)
        {
            try
            {
                var reviews = await _context.GameReviews
                    .Include(gr => gr.User)
                    .Where(gr => gr.GameID == gameId)
                    .Select(gr => new
                    {
                        gr.ReviewID,
                        gr.UserID,
                        Username = gr.User != null ? gr.User.Username : null,
                        gr.Rating
                    })
                    .ToListAsync();

                return Ok(reviews);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reviews for game {GameId}", gameId);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/review
        [HttpPost]
        public async Task<ActionResult<GameReview>> CreateReview(GameReview review)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if user already reviewed this game
                var existingReview = await _context.GameReviews
                    .FirstOrDefaultAsync(gr => gr.UserID == review.UserID && gr.GameID == review.GameID);
                
                if (existingReview != null)
                {
                    return BadRequest("User has already reviewed this game");
                }

                // Validate rating (1-5)
                if (review.Rating < 1 || review.Rating > 5)
                {
                    return BadRequest("Rating must be between 1 and 5");
                }

                _context.GameReviews.Add(review);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetReview), new { id = review.ReviewID }, review);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating review");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // PUT: api/review/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReview(int id, GameReview review)
        {
            try
            {
                if (id != review.ReviewID)
                {
                    return BadRequest("Review ID mismatch");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var existingReview = await _context.GameReviews.FindAsync(id);
                if (existingReview == null)
                {
                    return NotFound($"Review with ID {id} not found");
                }

                // Validate rating (1-5)
                if (review.Rating < 1 || review.Rating > 5)
                {
                    return BadRequest("Rating must be between 1 and 5");
                }

                existingReview.Rating = review.Rating;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating review with ID {ReviewId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // DELETE: api/review/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            try
            {
                var review = await _context.GameReviews.FindAsync(id);
                if (review == null)
                {
                    return NotFound($"Review with ID {id} not found");
                }

                _context.GameReviews.Remove(review);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting review with ID {ReviewId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
