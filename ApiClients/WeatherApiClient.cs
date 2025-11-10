using System.Net.Http;
using System.Text.Json;
using WeatherForge.Models;

namespace WeatherForge.ApiClients;

// Builder pattern: tillåter stegvis konfigurering av API-anropet
public class WeatherApiClient
{
    private readonly HttpClient _httpClient;
    private string _baseUrl = "https://api.open-meteo.com/v1/forecast";
    private double _latitude;
    private double _longitude;

    public WeatherApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // Builder-metoder
    public WeatherApiClient WithCoordinates(double latitude, double longitude)
    {
        _latitude = latitude;
        _longitude = longitude;
        return this;
    }

    public WeatherApiClient WithCustomUrl(string baseUrl)
    {
        _baseUrl = baseUrl;
        return this;
    }

    // Utför anropet
    public async Task<WeatherData?> GetWeatherAsync()
    {
        var url = $"{_baseUrl}?latitude={_latitude}&longitude={_longitude}&current_weather=true";

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var root = doc.RootElement.GetProperty("current_weather");

        return new WeatherData
        {
            City = "Custom location",
            Temperature = root.GetProperty("temperature").GetDouble(),
            Condition = "Unknown",
            Date = DateTime.Now
        };
    }
}
