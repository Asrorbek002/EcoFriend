using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

public class AdminProductModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string PackageInfo { get; set; } = string.Empty;
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class AdminSettingsModel
{
    public string Username { get; set; } = "admin";

    // SHU YERDAGI PAROLNI O'ZGARTIRISHINGIZ MUMKIN:
    public string Password { get; set; } = "adminpassword"; // Masalan, yangi parol

    public string AdTitle { get; set; } = "Xazonlardan Ekologik Briketlar";
    public string AdDescription { get; set; } = "Tabiatni asraymiz, uyingizni issiq qilamiz.";
    public string AdImageUrl { get; set; } = "";
}

public class AdminPanelService
{
    private readonly string _filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "admin_data.json");

    private class DatabaseContainer
    {
        public AdminSettingsModel Settings { get; set; } = new();
        public List<AdminProductModel> Products { get; set; } = new();
    }

    private DatabaseContainer LoadData()
    {
        if (!File.Exists(_filePath))
        {
            var initial = new DatabaseContainer();
            // Boshlang'ich mahsulotlar
            initial.Products.Add(new AdminProductModel { Id = 1, Name = "Eco Premium Zich", Description = "Uzoq vaqt yonuvchi premium mahsulot.", Price = 50000, PackageInfo = "Qadoq: 20 kg", CreatedAt = DateTime.Now });
            initial.Products.Add(new AdminProductModel { Id = 2, Name = "Eco Standard Briket", Description = "Kundalik uy va kaminlar uchun.", Price = 35000, PackageInfo = "Qadoq: 10 kg", CreatedAt = DateTime.Now });
            SaveData(initial);
            return initial;
        }

        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<DatabaseContainer>(json) ?? new DatabaseContainer();
    }

    private void SaveData(DatabaseContainer data)
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }

    // 1. Admin loginni tekshirish
    public bool Authenticate(string username, string password)
    {
        var data = LoadData();
        return data.Settings.Username == username && data.Settings.Password == password;
    }

    // 2. Login va parolni o'zgartirish
    public bool UpdateCredentials(string oldUser, string oldPass, string newUser, string newPass)
    {
        var data = LoadData();
        if (data.Settings.Username == oldUser && data.Settings.Password == oldPass)
        {
            data.Settings.Username = newUser;
            data.Settings.Password = newPass;
            SaveData(data);
            return true;
        }
        return false;
    }

    // 3. Mahsulot qo'shish
    public void AddProduct(string name, string desc, decimal price, string package)
    {
        var data = LoadData();
        int newId = data.Products.Any() ? data.Products.Max(p => p.Id) + 1 : 1;
        data.Products.Add(new AdminProductModel
        {
            Id = newId,
            Name = name,
            Description = desc,
            Price = price,
            PackageInfo = package,
            IsDeleted = false,
            CreatedAt = DateTime.Now
        });
        SaveData(data);
    }

    // Mahsulotni o'chirish (Soft delete)
    public void DeleteProduct(int id)
    {
        var data = LoadData();
        var product = data.Products.FirstOrDefault(p => p.Id == id);
        if (product != null)
        {
            product.IsDeleted = true;
            SaveData(data);
        }
    }

    // O'chirilgan mahsulotlarni tiklash yoki ko'rish
    public List<AdminProductModel> GetProducts(bool includeDeleted = false)
    {
        var data = LoadData();
        return includeDeleted ? data.Products : data.Products.Where(p => !p.IsDeleted).ToList();
    }

    // 4. Kunlik va oylik hisobotlar
    public object GetReport(string type, int year, int month, int? day = null)
    {
        var data = LoadData();
        var query = data.Products.Where(p => p.CreatedAt.Year == year && p.CreatedAt.Month == month);

        if (type == "daily" && day.HasValue)
        {
            query = query.Where(p => p.CreatedAt.Day == day.Value);
        }

        var list = query.ToList();
        return new
        {
            Period = type,
            TotalCreatedCount = list.Count,
            ActiveCount = list.Count(p => !p.IsDeleted),
            DeletedCount = list.Count(p => p.IsDeleted),
            Items = list
        };
    }

    // 5. Reklama sozlamalarini boshqarish va saytda aks ettirish
    public AdminSettingsModel GetSettings()
    {
        var data = LoadData();
        return data.Settings;
    }

    public void UpdateAdSettings(string title, string desc, string imageUrl)
    {
        var data = LoadData();
        data.Settings.AdTitle = title;
        data.Settings.AdDescription = desc;
        data.Settings.AdImageUrl = imageUrl;
        SaveData(data);
    }
}