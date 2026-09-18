using Web_sayt.Services;

var builder = WebApplication.CreateBuilder(args);

// Servislarni ro'yxatdan o'tkazish
builder.Services.AddControllers();
builder.Services.AddScoped<ExcelDbService>(); // Excel bilan xavfsiz ishlash uchun Scoped qilindi
builder.Services.AddSingleton<AdminPanelService>(); // <--- Mana bu yerga qo'shildi

var app = builder.Build();

// Middleware sozlamalari
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();

app.Run();