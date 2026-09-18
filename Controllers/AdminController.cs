using Microsoft.AspNetCore.Mvc;
using Web_sayt.Services;
using Web_sayt.Models;

namespace Web_sayt.Controllers
{
    [Route("api/admin")]
    [ApiController]
    public class AdminApiController : ControllerBase
    {
        private readonly AdminPanelService _adminService;
        private readonly ExcelDbService _dbService;

        public AdminApiController(AdminPanelService adminService, ExcelDbService dbService)
        {
            _adminService = adminService;
            _dbService = dbService;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] AdminLoginRequest model)
        {
            bool success = _adminService.Authenticate(model.Username, model.Password);
            if (!success) return Unauthorized(new { message = "Login yoki parol xato!" });
            return Ok(new { message = "Muvaffaqiyatli kirildi" });
        }

        [HttpPost("change-credentials")]
        public IActionResult ChangeCredentials([FromBody] ChangeCredentialsRequest model)
        {
            bool success = _adminService.UpdateCredentials(model.OldUsername, model.OldPassword, model.NewUsername, model.NewPassword);
            if (!success) return BadRequest(new { message = "Eski login yoki parol noto'g'ri!" });
            return Ok(new { message = "Login va parol muvaffaqiyatli o'zgartirildi" });
        }

        [HttpGet("products")]
        public IActionResult GetProducts([FromQuery] bool includeDeleted = false)
        {
            var products = _adminService.GetProducts(includeDeleted);
            return Ok(products);
        }

        [HttpPost("products")]
        public IActionResult AddProduct([FromBody] AddProductRequest model)
        {
            _adminService.AddProduct(model.Name, model.Description, model.Price, model.PackageInfo);
            return Ok(new { message = "Mahsulot muvaffaqiyatli yaratildi" });
        }

        [HttpDelete("products/{id}")]
        public IActionResult DeleteProduct(int id)
        {
            _adminService.DeleteProduct(id);
            return Ok(new { message = "Mahsulot o'chirildi" });
        }

        [HttpGet("reports")]
        public IActionResult GetReport([FromQuery] string type, [FromQuery] int year, [FromQuery] int month, [FromQuery] int? day = null)
        {
            var report = _adminService.GetReport(type, year, month, day);
            return Ok(report);
        }

        [HttpGet("settings")]
        public IActionResult GetSettings()
        {
            var settings = _adminService.GetSettings();
            return Ok(settings);
        }

        [HttpPost("settings/ads")]
        public IActionResult UpdateAds([FromBody] UpdateAdsRequest model)
        {
            _adminService.UpdateAdSettings(model.Title, model.Description, model.ImageUrl);
            return Ok(new { message = "Reklama sozlamalari yangilandi" });
        }

        // --- YANGI: Admin panelida "Ro'yxatdan o'tgan foydalanuvchilar" jadvali
        // doim bo'sh chiqayotgan edi, chunki bu endpoint umuman yo'q edi.
        // Endi haqiqiy ro'yxatni Excel fayldan olib, parolsiz (xavfsiz) qaytaradi.
        [HttpGet("users")]
        public IActionResult GetUsers()
        {
            var users = _dbService.GetAllUsers()
                .Select(u => new UserPublicDto { Name = u.Name, Phone = u.Phone })
                .ToList();

            return Ok(new { count = users.Count, users });
        }
    }

    // DTO modellari
    // Eslatma: "LoginRequest" nomi Models papkasidagi LoginRequest bilan
    // chalkashib ketmasligi uchun "AdminLoginRequest" deb o'zgartirildi.
    public class AdminLoginRequest { public string Username { get; set; } = ""; public string Password { get; set; } = ""; }
    public class ChangeCredentialsRequest { public string OldUsername { get; set; } = ""; public string OldPassword { get; set; } = ""; public string NewUsername { get; set; } = ""; public string NewPassword { get; set; } = ""; }
    public class AddProductRequest { public string Name { get; set; } = ""; public string Description { get; set; } = ""; public decimal Price { get; set; } public string PackageInfo { get; set; } = ""; }
    public class UpdateAdsRequest { public string Title { get; set; } = ""; public string Description { get; set; } = ""; public string ImageUrl { get; set; } = ""; }
}
