using Microsoft.AspNetCore.Mvc;
using GameRash.Data;
using GameRash.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace GameRash.Controllers
{
    public class AuthController : Controller
    {
        private readonly GameRashDbContext _context;
        private readonly ILogger<AuthController> _logger;

        public AuthController(GameRashDbContext context, ILogger<AuthController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /auth/login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /auth/login
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            try
            {
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    TempData["ErrorMessage"] = "Kullanıcı adı ve şifre gereklidir.";
                    return View();
                }

                var hashedPassword = HashPassword(password);
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username && u.Password == hashedPassword);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "Kullanıcı adı veya şifre hatalı.";
                    return View();
                }

                // Session'a kullanıcı bilgilerini kaydet
                HttpContext.Session.SetString("UserId", user.UserID.ToString());
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Email", user.Email);

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error");
                TempData["ErrorMessage"] = "Giriş yapılırken bir hata oluştu.";
                return View();
            }
        }

        // GET: /auth/register
        public IActionResult Register()
        {
            return View();
        }

        // POST: /auth/register
        [HttpPost]
        public async Task<IActionResult> Register(string username, string email, string password, string confirmPassword)
        {
            try
            {
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || 
                    string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
                {
                    TempData["ErrorMessage"] = "Tüm alanlar gereklidir.";
                    return View();
                }

                if (password != confirmPassword)
                {
                    TempData["ErrorMessage"] = "Şifreler eşleşmiyor.";
                    return View();
                }

                if (password.Length < 6)
                {
                    TempData["ErrorMessage"] = "Şifre en az 6 karakter olmalıdır.";
                    return View();
                }

                // Kullanıcı adı ve email kontrolü
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username || u.Email == email);

                if (existingUser != null)
                {
                    TempData["ErrorMessage"] = "Bu kullanıcı adı veya email zaten kullanılıyor.";
                    return View();
                }

                var hashedPassword = HashPassword(password);
                var newUser = new User
                {
                    Username = username,
                    Email = email,
                    Password = hashedPassword
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Kayıt başarılı! Şimdi giriş yapabilirsiniz.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Register error");
                TempData["ErrorMessage"] = "Kayıt olurken bir hata oluştu.";
                return View();
            }
        }

        // GET: /auth/logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}
