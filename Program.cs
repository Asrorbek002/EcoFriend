using Web_sayt.Services;

var builder = WebApplication.CreateBuilder(args);

// Servislarni ro'yxatdan o'tkazish
builder.Services.AddControllers();
builder.Services.AddSingleton<ExcelDbService>();
builder.Services.AddSingleton<AdminPanelService>(); // <--- Mana bu yerga qo'shildi

var app = builder.Build();

// Middleware sozlamalari
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();

app.Run();