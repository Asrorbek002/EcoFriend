using Web_sayt.Models;

namespace Web_sayt.Services
{
    public class ExcelDbService
    {
        private static readonly List<User> UsersDb = new();

        public List<Product> GetProducts()
        {
            return new List<Product>
            {
                new Product { Id = 1, Name = "Standart Barg Briket", Price = 2500, Description = "Xonadonlar va pechlar uchun" },
                new Product { Id = 2, Name = "Premium Briket (Presslangan)", Price = 3200, Description = "Sanoat ob'ektlari uchun" }
            };
        }

        public bool RegisterUser(User user)
        {
            if (UsersDb.Any(u => u.Phone == user.Phone))
            {
                return false; // Telefon raqam allaqachon bor
            }
            UsersDb.Add(user);
            return true;
        }

        public User? ValidateUser(string phone, string password)
        {
            return UsersDb.FirstOrDefault(u => u.Phone == phone && u.Password == password);
        }
    }
}