using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.Extensions.Caching.Memory;
using WeatherInsight.Models;

namespace WeatherInsight.ApiClients
{
    public class WeatherApiClient : IWeatherApiClient
    {
        private readonly HttpClient _client;
        private readonly WeatherRequestBuilder _builder;
        private readonly string _apiKey;
        private readonly IMemoryCache _cache;

        public WeatherApiClient(HttpClient client, WeatherRequestBuilder builder, IConfiguration config, IMemoryCache cache)
        {
            _client = client;
            _builder = builder;
            _cache = cache;
            _apiKey = Environment.GetEnvironmentVariable("OPENWEATHER_API_KEY");
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("Missing OpenWeather API key. Configure 'OPENWEATHER_API_KEY' via environment variables.");
            }
        }

        public async Task<WeatherData?> FetchWeatherAsync(string city)
        {
            var url = _builder.WithCity(city).Build(_apiKey);

            // Variable to hold the JSON response
            string json;

            var cacheKey = $"weatherdata_{city}";

            // Look for weatherdata in memory cache
            if (_cache.TryGetValue(cacheKey, out WeatherData? cachedWeatherData))
            {
                   // The program expects the Id of new searches to be 0.
                   //  The Id of the cached data is already taken in the search history weather.db database.
                    cachedWeatherData.Id = 0;
                    return cachedWeatherData;
            }

            // Attempt to fetch the weather data
            try
            {
                json = await _client.GetStringAsync(url);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // If it returns 404, the city was not found, throw a clear error message
                throw new ArgumentException("City name is wrong");
            }

            var obj = JsonSerializer.Deserialize<JsonElement>(json);
            var newWeatherData = new WeatherData
            {
                City = city,
                Temperature = obj.GetProperty("main").GetProperty("temp").GetDouble(),
                Condition = obj.GetProperty("weather")[0].GetProperty("description").GetString() ?? "",
                Date = DateTime.Now
            };

            // Store the newly optained weatherdata in memory cacke.
            _cache.Set(cacheKey, newWeatherData, TimeSpan.FromMinutes(15));

            return newWeatherData;
        }
    }
}
