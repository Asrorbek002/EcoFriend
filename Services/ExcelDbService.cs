using OfficeOpenXml; // EPPlus uchun
using Web_sayt.Models;

namespace Web_sayt.Services
{
    public class ExcelDbService
    {
        private readonly string _filePath;

        public ExcelDbService(IWebHostEnvironment env)
        {
            // Excel faylning yo'lini aniqlaymiz (Data papkasi ichida database.xlsx)
            _filePath = Path.Combine(env.ContentRootPath, "Data", "database.xlsx");

            // EPPlus litsenziya sozlamasi (non-commercial uchun)
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

        // Barcha foydalanuvchilarni Excel fayldan o'qish
        public List<User> GetAllUsers()
        {
            var users = new List<User>();

            if (!File.Exists(_filePath))
                return users;

            using (var package = new ExcelPackage(new FileInfo(_filePath)))
            {
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null || worksheet.Dimension == null)
                    return users;

                int rowCount = worksheet.Dimension.Rows;

                // 2-qatordan boshlaymiz (1-qator sarlavha: Ism, Telefon, Parol)
                for (int row = 2; row <= rowCount; row++)
                {
                    var user = new User
                    {
                        Name = worksheet.Cells[row, 1].Text,
                        Phone = worksheet.Cells[row, 2].Text,
                        Password = worksheet.Cells[row, 3].Text
                    };

                    if (!string.IsNullOrEmpty(user.Phone))
                    {
                        users.Add(user);
                    }
                }
            }

            return users;
        }

        // Yangi foydalanuvchini Excel faylga yozish
        public bool RegisterUser(User user)
        {
            var users = GetAllUsers();

            // Telefon raqam oldindan bormi tekshiramiz
            if (users.Any(u => u.Phone == user.Phone))
            {
                return false;
            }

            FileInfo fileInfo = new FileInfo(_filePath);

            // Agar Data papkasi yoki fayl yo'q bo'lsa yaratib olamiz
            if (!Directory.Exists(fileInfo.DirectoryName))
            {
                Directory.CreateDirectory(fileInfo.DirectoryName!);
            }

            using (var package = new ExcelPackage(fileInfo))
            {
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                {
                    worksheet = package.Workbook.Worksheets.Add("Users");
                    // Sarlavhalar yozamiz
                    worksheet.Cells[1, 1].Value = "Name";
                    worksheet.Cells[1, 2].Value = "Phone";
                    worksheet.Cells[1, 3].Value = "Password";
                }

                int newRow = worksheet.Dimension?.Rows + 1 ?? 2;

                worksheet.Cells[newRow, 1].Value = user.Name;
                worksheet.Cells[newRow, 2].Value = user.Phone;
                worksheet.Cells[newRow, 3].Value = user.Password;

                package.Save();
            }

            return true;
        }

        public User? ValidateUser(string phone, string password)
        {
            var users = GetAllUsers();
            return users.FirstOrDefault(u => u.Phone == phone && u.Password == password);
        }
    }
}