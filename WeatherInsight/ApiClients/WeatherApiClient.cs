using System.Text.Json;
using WeatherInsight.Models;

namespace WeatherInsight.ApiClients
{
    public class WeatherApiClient : IWeatherApiClient
    {
        private readonly HttpClient _client;
        private readonly WeatherRequestBuilder _builder;
        private readonly string _apiKey;

        public WeatherApiClient(HttpClient client, WeatherRequestBuilder builder, IConfiguration config)
        {
            _client = client;
            _builder = builder;
            _apiKey = config["OpenWeather:ApiKey"] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new InvalidOperationException("Missing OpenWeather API key. Configure 'OpenWeather:ApiKey' via environment variables or user secrets.");
            }
        }

        public async Task<WeatherData?> FetchWeatherAsync(string city)
        {
            var url = _builder.WithCity(city).Build(_apiKey);
            var json = await _client.GetStringAsync(url);
            var obj = JsonSerializer.Deserialize<JsonElement>(json);

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
