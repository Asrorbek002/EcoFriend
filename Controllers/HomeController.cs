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
        private const string TelegramBotToken = "8920465545:AAGroaLPm0CJfy1LacDjjrhlm5835Ow-LwY";
        private const string TelegramChatId = "5399843682";

        public HomeController(ExcelDbService dbService)
        {
            _dbService = dbService;
        }

        [HttpGet("products")]
        public IActionResult GetProducts()
        {
            var products = _dbService.GetProducts();
            return Ok(products);
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] User user)
        {
            if (string.IsNullOrEmpty(user.Name) || string.IsNullOrEmpty(user.Phone) || string.IsNullOrEmpty(user.Password))
            {
                return BadRequest("Barcha maydonlarni to'ldiring.");
            }

            bool success = _dbService.RegisterUser(user);
            if (!success)
            {
                return BadRequest("Ushbu telefon raqam allaqachon ro'yxatdan o'tgan!");
            }

            return Ok(new { message = "Muvaffaqiyatli ro'yxatdan o'tdingiz!" });
        }
        public class LoginRequest
        {
            public string Phone { get; set; }
            public string Password { get; set; }
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest login)
        {
            var user = _dbService.ValidateUser(login.Phone, login.Password);
            if (user == null)
            {
                return NotFound(new { message = "Bunday foydalanuvchi topilmadi. Iltimos, ro'yxatdan o'ting!" });
            }

            return Ok(new { name = user.Name, phone = user.Phone });
        }

        // --- BUYURTMA QABUL QILISH VA TELEGRAMGA YUBORISH METODI (Xatolikni tekshiruvchi) ---
        [HttpPost("order")]
        public async Task<IActionResult> CreateOrder([FromBody] OrderModel orderDto)
        {
            if (orderDto == null || string.IsNullOrEmpty(orderDto.Name) || string.IsNullOrEmpty(orderDto.Phone))
            {
                return BadRequest(new { message = "Buyurtma ma'lumotlari to'liq emas!" });
            }

            try
            {
                string message = $"🛒 *Yangi buyurtma keldi!*\n\n" +
                                 $"👤 *Mijoz:* {orderDto.Name}\n" +
                                 $"📞 *Telefon:* {orderDto.Phone}\n" +
                                 $"📦 *Mahsulot:* {orderDto.Product}";

                string url = $"https://api.telegram.org/bot{TelegramBotToken}/sendMessage?chat_id={TelegramChatId}&text={Uri.EscapeDataString(message)}&parse_mode=Markdown";

                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync(url);
                    string responseString = await response.Content.ReadAsStringAsync();

                    // Agar Telegram xato bersa, o'sha xatoni to'g'ridan-to'g'ri qaytaradi
                    if (!response.IsSuccessStatusCode)
                    {
                        return StatusCode(500, new { message = "Telegram xatosi: " + responseString });
                    }
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
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Product { get; set; }
    }
}