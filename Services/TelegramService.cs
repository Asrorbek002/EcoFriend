namespace Web_sayt.Services
{
    public interface ITelegramService
    {
        Task SendOrderAsync(string message);
    }

    public class TelegramService : ITelegramService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public TelegramService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task SendOrderAsync(string message)
        {
            var botToken = _config["TelegramSettings:BotToken"];
            var chatId = _config["TelegramSettings:ChatId"];

            if (string.IsNullOrEmpty(botToken) || string.IsNullOrEmpty(chatId)) return;

            var url = $"https://api.telegram.org/bot{botToken}/sendMessage";
            var payload = new
            {
                chat_id = chatId,
                text = message,
                parse_mode = "Markdown"
            };

            await _httpClient.PostAsJsonAsync(url, payload);
        }
    }
}