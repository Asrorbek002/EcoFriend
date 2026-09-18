using OfficeOpenXml; // EPPlus uchun
using System.Security.Cryptography;
using System.Text;
using Web_sayt.Models;

namespace Web_sayt.Services
{
    public class ExcelDbService
    {
        private readonly string _filePath;

        // MUHIM: Bir vaqtning o'zida bir nechta so'rov Excel faylga
        // kirishga urinishining oldini oladi (concurrency muammosini hal qiladi).
        // Aynan lock yo'qligi "boshqa foydalanuvchilar ro'yxatdan o'tganda xatolik"
        // muammosining asosiy sababi edi.
        private static readonly object _fileLock = new object();

        public ExcelDbService(IWebHostEnvironment env)
        {
            _filePath = Path.Combine(env.ContentRootPath, "Data", "database.xlsx");
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public List<Product> GetProducts()
        {
            return new List<Product>
            {
                new Product { Id = 1, Name = "Standart Barg Briket", Price = 2500, Description = "Xonadonlar va pechlar uchun" },
                new Product { Id = 2, Name = "Premium Briket (Presslangan)", Price = 3200, Description = "Sanoat ob'ektlari uchun" }
            };
        }

        // Telefon raqamni bir xil formatga keltiramiz: faqat raqamlar qoladi.
        // Aks holda "+998901234567" va "998 90 123 45 67" ikki xil
        // foydalanuvchi sifatida saqlanib, login/ro'yxatdan o'tishda chalkashlik keltirib chiqaradi.
        private static string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return string.Empty;
            return new string(phone.Where(char.IsDigit).ToArray());
        }

        // Parolni ochiq matn holida saqlamaslik uchun hash qilamiz.
        // Eslatma: bu SHA-256 - eskisidan ancha xavfsiz, lekin production uchun
        // BCrypt.Net-Next kutubxonasidan foydalanish tavsiya etiladi.
        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes);
        }

        public List<User> GetAllUsers()
        {
            lock (_fileLock)
            {
                return GetAllUsersInternal();
            }
        }

        // Lock ICHIDA chaqirish uchun - qayta-qayta lock olib, o'z-o'zini
        // to'xtatib qo'yishning (deadlock) oldini oladi.
        private List<User> GetAllUsersInternal()
        {
            var users = new List<User>();

            if (!File.Exists(_filePath))
                return users;

            try
            {
                using var package = new ExcelPackage(new FileInfo(_filePath));
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null || worksheet.Dimension == null)
                    return users;

                int rowCount = worksheet.Dimension.Rows;

                // 2-qatordan boshlaymiz (1-qator sarlavha: Name, Phone, Password)
                for (int row = 2; row <= rowCount; row++)
                {
                    var phone = worksheet.Cells[row, 2].Text;
                    if (string.IsNullOrWhiteSpace(phone)) continue;

                    users.Add(new User
                    {
                        Name = worksheet.Cells[row, 1].Text,
                        Phone = phone,
                        Password = worksheet.Cells[row, 3].Text
                    });
                }
            }
            catch (Exception ex)
            {
                // Fayl vaqtincha band bo'lib qolsa ham server yiqilmasin
                Console.WriteLine($"[ExcelDbService] Excel o'qishda xato: {ex.Message}");
            }

            return users;
        }

        // Endi (bool, string) qaytaradi - foydalanuvchiga aniq xato xabarini
        // ko'rsatish imkonini beradi ("Success" bool, "Error" - xato matni)
        public (bool Success, string Error) RegisterUser(User user)
        {
            var normalizedPhone = NormalizePhone(user.Phone);
            if (normalizedPhone.Length < 9)
                return (false, "Telefon raqam noto'g'ri formatda.");

            lock (_fileLock)
            {
                try
                {
                    var users = GetAllUsersInternal();

                    if (users.Any(u => NormalizePhone(u.Phone) == normalizedPhone))
                        return (false, "Ushbu telefon raqam allaqachon ro'yxatdan o'tgan!");

                    var fileInfo = new FileInfo(_filePath);

                    if (!Directory.Exists(fileInfo.DirectoryName))
                        Directory.CreateDirectory(fileInfo.DirectoryName!);

                    using var package = new ExcelPackage(fileInfo);
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();

                    if (worksheet == null)
                    {
                        worksheet = package.Workbook.Worksheets.Add("Users");
                        worksheet.Cells[1, 1].Value = "Name";
                        worksheet.Cells[1, 2].Value = "Phone";
                        worksheet.Cells[1, 3].Value = "Password";
                    }

                    int newRow = (worksheet.Dimension?.Rows ?? 1) + 1;

                    worksheet.Cells[newRow, 1].Value = user.Name;
                    worksheet.Cells[newRow, 2].Value = normalizedPhone;
                    worksheet.Cells[newRow, 3].Value = HashPassword(user.Password);

                    package.Save();

                    return (true, string.Empty);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ExcelDbService] Ro'yxatdan o'tishda xato: {ex.Message}");
                    return (false, "Serverda vaqtinchalik xatolik yuz berdi. Birozdan so'ng qayta urinib ko'ring.");
                }
            }
        }

        public User? ValidateUser(string phone, string password)
        {
            var normalizedPhone = NormalizePhone(phone);
            var hashedInput = HashPassword(password);

            lock (_fileLock)
            {
                var users = GetAllUsersInternal();
                return users.FirstOrDefault(u =>
                    NormalizePhone(u.Phone) == normalizedPhone && u.Password == hashedInput);
            }
        }
    }
}