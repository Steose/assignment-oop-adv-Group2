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