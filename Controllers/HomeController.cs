using Microsoft.AspNetCore.Mvc;
using Web_sayt.Models;
using Web_sayt.Services;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Web_sayt.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly ExcelDbService _dbService;
        private readonly IConfiguration _config;

        public HomeController(ExcelDbService dbService, IConfiguration config)
        {
            _dbService = dbService;
            _config = config;
        }

        [HttpGet("products")]
        public IActionResult GetProducts()
        {
            var products = _dbService.GetProducts();
            return Ok(products);
        }

        // Jami foydalanuvchilar sonini qaytaradi - bosh sahifadagi
        // "Foydalanuvchilar" statistikasi shu yerdan olinadi (hammaga ko'rinadi)
        [HttpGet("users-count")]
        public IActionResult GetUsersCount()
        {
            var users = _dbService.GetAllUsers();
            return Ok(new { count = users.Count });
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] User user)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.Name) ||
                string.IsNullOrWhiteSpace(user.Phone) || string.IsNullOrWhiteSpace(user.Password))
            {
                return BadRequest(new { message = "Barcha maydonlarni to'ldiring." });
            }

            // Endi ExcelDbService lock bilan ishlaydi va aniq xato xabarini qaytaradi
            var (success, error) = _dbService.RegisterUser(user);
            if (!success)
            {
                return BadRequest(new { message = error });
            }

            return Ok(new { message = "Muvaffaqiyatli ro'yxatdan o'tdingiz!" });
        }

        // Endi Models.LoginRequest ishlatiladi - controller ichida
        // takrorlangan class olib tashlandi
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest login)
        {
            if (login == null || string.IsNullOrWhiteSpace(login.Username) || string.IsNullOrWhiteSpace(login.Password))
            {
                return BadRequest(new { message = "Telefon va parolni kiriting." });
            }

            var user = _dbService.ValidateUser(login.Username, login.Password);
            if (user == null)
            {
                return NotFound(new { message = "Bunday foydalanuvchi topilmadi. Iltimos, ro'yxatdan o'ting!" });
            }

            return Ok(new { name = user.Name, phone = user.Phone });
        }

        // --- BUYURTMA QABUL QILISH VA TELEGRAMGA YUBORISH METODI ---
        [HttpPost("order")]
        public async Task<IActionResult> CreateOrder([FromBody] OrderModel orderDto)
        {
            if (orderDto == null || string.IsNullOrEmpty(orderDto.Name) || string.IsNullOrEmpty(orderDto.Phone))
            {
                return BadRequest(new { message = "Buyurtma ma'lumotlari to'liq emas!" });
            }

            // MUHIM: Token endi kodda emas, appsettings.json / environment variable'dan olinadi
            var telegramBotToken = _config["Telegram:BotToken"];
            var telegramChatId = _config["Telegram:ChatId"];

            if (string.IsNullOrEmpty(telegramBotToken) || string.IsNullOrEmpty(telegramChatId))
            {
                return StatusCode(500, new { message = "Telegram sozlamalari topilmadi. appsettings.json yoki Render Environment Variables'ni tekshiring." });
            }

            try
            {
                string message = $"🛒 *Yangi buyurtma keldi!*\n\n" +
                                 $"👤 *Mijoz:* {orderDto.Name}\n" +
                                 $"📞 *Telefon:* {orderDto.Phone}\n" +
                                 $"📦 *Mahsulot:* {orderDto.Product}";

                string url = $"https://api.telegram.org/bot{telegramBotToken}/sendMessage?chat_id={telegramChatId}&text={Uri.EscapeDataString(message)}&parse_mode=Markdown";

                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(url);
                string responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode(500, new { message = "Telegram xatosi: " + responseString });
                }

                return Ok(new { message = "Buyurtmangiz muvaffaqiyatli qabul qilindi va operatorlarga yuborildi!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Server xatosi: " + ex.Message });
            }
        }
    }

    public class OrderModel
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Product { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Username { get; set; }    // keep if other code uses it
        public string Phone { get; set; }       // added to match controller
        public string Password { get; set; }
    }
}