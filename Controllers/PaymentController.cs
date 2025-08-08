using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameRash.Data;
using GameRash.Models;

namespace GameRash.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly GameRashDbContext _context;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(GameRashDbContext context, ILogger<PaymentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/payment
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetPayments()
        {
            try
            {
                var payments = await _context.Payments
                    .Include(p => p.Purchase)
                        .ThenInclude(pur => pur.User)
                    .Include(p => p.Purchase)
                        .ThenInclude(pur => pur.Game)
                    .Select(p => new
                    {
                        p.PaymentID,
                        p.PurchaseID,
                        p.PaymentMethod,
                        p.PaymentDate,
                        p.Status,
                        Amount = p.Purchase != null ? CalculateAmount(p.Purchase.GameID) : 0, // You'll need to implement price logic
                        Username = p.Purchase != null && p.Purchase.User != null ? p.Purchase.User.Username : null,
                        GameTitle = p.Purchase != null && p.Purchase.Game != null ? p.Purchase.Game.Title : null
                    })
                    .ToListAsync();

                return Ok(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payments");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/payment/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetPayment(int id)
        {
            try
            {
                var payment = await _context.Payments
                    .Include(p => p.Purchase)
                        .ThenInclude(pur => pur.User)
                    .Include(p => p.Purchase)
                        .ThenInclude(pur => pur.Game)
                    .Where(p => p.PaymentID == id)
                    .Select(p => new
                    {
                        p.PaymentID,
                        p.PurchaseID,
                        p.PaymentMethod,
                        p.PaymentDate,
                        p.Status,
                        Amount = CalculateAmount(p.Purchase.GameID),
                        Purchase = new
                        {
                            p.Purchase.PurchaseID,
                            p.Purchase.PurchaseDate,
                            Username = p.Purchase.User != null ? p.Purchase.User.Username : null,
                            GameTitle = p.Purchase.Game != null ? p.Purchase.Game.Title : null
                        }
                    })
                    .FirstOrDefaultAsync();

                if (payment == null)
                {
                    return NotFound($"Payment with ID {id} not found");
                }

                return Ok(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment with ID {PaymentId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/payment/purchase/5
        [HttpGet("purchase/{purchaseId}")]
        public async Task<ActionResult<IEnumerable<Payment>>> GetPaymentsByPurchase(int purchaseId)
        {
            try
            {
                var payments = await _context.Payments
                    .Where(p => p.PurchaseID == purchaseId)
                    .ToListAsync();

                return Ok(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payments for purchase {PurchaseId}", purchaseId);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/payment/user/5
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetPaymentsByUser(int userId)
        {
            try
            {
                var payments = await _context.Payments
                    .Include(p => p.Purchase)
                        .ThenInclude(pur => pur.Game)
                    .Where(p => p.Purchase.UserID == userId)
                    .Select(p => new
                    {
                        p.PaymentID,
                        p.PurchaseID,
                        p.PaymentMethod,
                        p.PaymentDate,
                        p.Status,
                        Amount = CalculateAmount(p.Purchase.GameID),
                        GameTitle = p.Purchase.Game != null ? p.Purchase.Game.Title : null
                    })
                    .OrderByDescending(p => p.PaymentDate)
                    .ToListAsync();

                return Ok(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payments for user {UserId}", userId);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/payment
        [HttpPost]
        public async Task<ActionResult<Payment>> ProcessPayment(PaymentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Validate purchase exists
                var purchase = await _context.Purchases
                    .Include(p => p.Game)
                    .FirstOrDefaultAsync(p => p.PurchaseID == request.PurchaseID);

                if (purchase == null)
                {
                    return BadRequest("Purchase not found");
                }

                // Check if payment already exists for this purchase
                var existingPayment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.PurchaseID == request.PurchaseID && p.Status == "Completed");

                if (existingPayment != null)
                {
                    return BadRequest("Payment already processed for this purchase");
                }

                // Process payment (integrate with actual payment gateway here)
                var paymentResult = await ProcessPaymentWithGateway(request);

                if (!paymentResult.Success)
                {
                    return BadRequest($"Payment failed: {paymentResult.ErrorMessage}");
                }

                // Create payment record
                var payment = new Payment
                {
                    PurchaseID = request.PurchaseID,
                    PaymentMethod = request.PaymentMethod,
                    PaymentDate = DateTime.UtcNow,
                    Status = paymentResult.Success ? "Completed" : "Failed"
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                // If payment successful, update purchase status or any other business logic
                if (paymentResult.Success)
                {
                    // Add any post-payment logic here
                    _logger.LogInformation("Payment processed successfully for Purchase ID: {PurchaseId}", request.PurchaseID);
                }

                return CreatedAtAction(nameof(GetPayment), new { id = payment.PaymentID }, payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // PUT: api/payment/5/status
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdatePaymentStatus(int id, PaymentStatusUpdate statusUpdate)
        {
            try
            {
                var payment = await _context.Payments.FindAsync(id);
                if (payment == null)
                {
                    return NotFound($"Payment with ID {id} not found");
                }

                payment.Status = statusUpdate.Status;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Payment status updated to {Status} for Payment ID: {PaymentId}", statusUpdate.Status, id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment status for ID {PaymentId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/payment/5/refund
        [HttpPost("{id}/refund")]
        public async Task<ActionResult<object>> ProcessRefund(int id, RefundRequest request)
        {
            try
            {
                var payment = await _context.Payments
                    .Include(p => p.Purchase)
                    .FirstOrDefaultAsync(p => p.PaymentID == id);

                if (payment == null)
                {
                    return NotFound($"Payment with ID {id} not found");
                }

                if (payment.Status != "Completed")
                {
                    return BadRequest("Can only refund completed payments");
                }

                // Process refund with payment gateway
                var refundResult = await ProcessRefundWithGateway(payment, request.Amount, request.Reason);

                if (!refundResult.Success)
                {
                    return BadRequest($"Refund failed: {refundResult.ErrorMessage}");
                }

                // Update payment status
                payment.Status = request.Amount >= CalculateAmount(payment.Purchase.GameID) ? "Refunded" : "Partially Refunded";
                await _context.SaveChangesAsync();

                var result = new
                {
                    Message = "Refund processed successfully",
                    PaymentID = id,
                    RefundAmount = request.Amount,
                    NewStatus = payment.Status,
                    RefundTransactionId = refundResult.TransactionId
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund for payment ID {PaymentId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/payment/statistics
        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetPaymentStatistics()
        {
            try
            {
                var totalPayments = await _context.Payments
                    .Where(p => p.Status == "Completed")
                    .CountAsync();

                var totalRevenue = await _context.Purchases
                    .Include(p => p.Payments)
                    .Where(p => p.Payments.Any(pay => pay.Status == "Completed"))
                    .SumAsync(p => CalculateAmount(p.GameID));

                var paymentMethodStats = await _context.Payments
                    .Where(p => p.Status == "Completed")
                    .GroupBy(p => p.PaymentMethod)
                    .Select(g => new
                    {
                        PaymentMethod = g.Key,
                        Count = g.Count(),
                        Percentage = Math.Round((double)g.Count() / totalPayments * 100, 2)
                    })
                    .ToListAsync();

                var monthlyRevenue = await _context.Payments
                    .Where(p => p.Status == "Completed")
                    .Include(p => p.Purchase)
                    .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Revenue = g.Sum(p => CalculateAmount(p.Purchase.GameID)),
                        TransactionCount = g.Count()
                    })
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToListAsync();

                var failedPayments = await _context.Payments
                    .Where(p => p.Status == "Failed")
                    .CountAsync();

                return Ok(new
                {
                    TotalPayments = totalPayments,
                    TotalRevenue = totalRevenue,
                    FailedPayments = failedPayments,
                    SuccessRate = totalPayments > 0 ? Math.Round((double)totalPayments / (totalPayments + failedPayments) * 100, 2) : 0,
                    PaymentMethodStats = paymentMethodStats,
                    MonthlyRevenue = monthlyRevenue
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment statistics");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // Private helper methods
        private decimal CalculateAmount(int gameId)
        {
            // This is a placeholder - implement your actual pricing logic
            // You might want to add a Price field to your Game model
            // or have a separate pricing service
            return 59.99m; // Default game price
        }

        private async Task<PaymentResult> ProcessPaymentWithGateway(PaymentRequest request)
        {
            // Implement actual payment gateway integration here
            // For now, this is a mock implementation
            await Task.Delay(100); // Simulate API call

            // Mock success for demonstration
            return new PaymentResult
            {
                Success = true,
                TransactionId = Guid.NewGuid().ToString(),
                ErrorMessage = null
            };
        }

        private async Task<RefundResult> ProcessRefundWithGateway(Payment payment, decimal amount, string reason)
        {
            // Implement actual refund gateway integration here
            await Task.Delay(100); // Simulate API call

            return new RefundResult
            {
                Success = true,
                TransactionId = Guid.NewGuid().ToString(),
                ErrorMessage = null
            };
        }
    }

    // DTOs for payment operations
    public class PaymentRequest
    {
        public int PurchaseID { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? CardNumber { get; set; }
        public string? ExpiryDate { get; set; }
        public string? CVV { get; set; }
        public string? BillingAddress { get; set; }
    }

    public class PaymentStatusUpdate
    {
        public string Status { get; set; } = string.Empty;
    }

    public class RefundRequest
    {
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string? TransactionId { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class RefundResult
    {
        public bool Success { get; set; }
        public string? TransactionId { get; set; }
        public string? ErrorMessage { get; set; }
    }
}