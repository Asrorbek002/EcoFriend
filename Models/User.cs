namespace Web_sayt.Models
{
    public class User
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Phone { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Price { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    // YANGI: Admin panelida foydalanuvchilar ro'yxatini ko'rsatish uchun
    // parolni o'z ichiga OLMAYDIGAN xavfsiz DTO
    public class UserPublicDto
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }
}