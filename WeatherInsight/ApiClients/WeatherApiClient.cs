using System.Net;
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
                throw new InvalidOperationException(
                    "Missing OpenWeather API key. Configure 'OpenWeather:ApiKey' via environment variables or user secrets.");
            }
        }

        public async Task<WeatherData?> FetchWeatherAsync(string city)
        {
            var url = _builder.WithCity(city).Build(_apiKey);

            try
            {
                // Use GetAsync so we can inspect the status code first
                using var response = await _client.GetAsync(url);

                if (response.StatusCode == HttpStatusCode.NotFound ||
                    !response.IsSuccessStatusCode)
                {
                    // City not found or other API error → just return null
                    return null;
                }

                var stream = await response.Content.ReadAsStreamAsync();
                var obj = await JsonSerializer.DeserializeAsync<JsonElement>(stream);

                // OpenWeather sometimes returns error info in JSON with "cod" != 200
                if (obj.TryGetProperty("cod", out var codElement) &&
                    codElement.ValueKind == JsonValueKind.String &&
                    codElement.GetString() != "200")
                {
                    return null;
                }

                return new WeatherData
                {
                    City = city,
                    Temperature = obj.GetProperty("main").GetProperty("temp").GetDouble(),
                    Condition = obj.GetProperty("weather")[0].GetProperty("description").GetString() ?? "",
                    Date = DateTime.Now
                };
            }
            catch (HttpRequestException)
            {
                // Network/DNS/etc → treat as "no data" instead of crash
                return null;
            }
            catch (JsonException)
            {
                // Unexpected JSON shape → also treat as "no data"
                return null;
            }
        }
    }
}
