using System.Text.Json;
using WeatherInsight.Models;

namespace WeatherInsight.ApiClients
{
    public class WeatherApiClient : IWeatherApiClient
    {
        private readonly HttpClient _client;
        private readonly WeatherRequestBuilder _builder;
        private CachedWeatherData _cacheDb;
        private readonly string _apiKey;

        public WeatherApiClient(HttpClient client, WeatherRequestBuilder builder, IConfiguration config, CachedWeatherData cacheDb )
        {
            _client = client;
            _builder = builder;
            _cacheDb = cacheDb;
            _apiKey = Environment.GetEnvironmentVariable("OPENWEATHER_API_KEY");
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("Missing OpenWeather API key. Configure 'OPENWEATHER_API_KEY' via environment variables.");
            }
        }

        public async Task<WeatherData?> FetchWeatherAsync(string city)
        {
            var url = _builder.WithCity(city).Build(_apiKey);
            var json = await _client.GetStringAsync(url);
            var obj = JsonSerializer.Deserialize<JsonElement>(json);

            var cachedWeatherData = await _cacheDb.CachedWeatherData
                .Where(m => m.City == city && m.ExpiresAt > DateTime.Now)
                .FirstOrDefaultAsync();

            if (cachedWeatherData != null) return cachedWeatherData;

            return new WeatherData
            {
                City = city,
                Temperature = obj.GetProperty("main").GetProperty("temp").GetDouble(),
                Condition = obj.GetProperty("weather")[0].GetProperty("description").GetString() ?? "",
                Date = DateTime.Now
            };
        }
    }
}
