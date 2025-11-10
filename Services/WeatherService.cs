using WeatherForge.ApiClients;
using WeatherForge.Data;
using WeatherForge.Models;
using Microsoft.EntityFrameworkCore;

namespace WeatherForge.Services;

// Repository + Dependency Injection
public class WeatherService
{
    private readonly AppDbContext _context;
    private readonly WeatherApiClient _apiClient;

    public WeatherService(AppDbContext context, WeatherApiClient apiClient)
    {
        _context = context;
        _apiClient = apiClient;
    }

    // Hämtar data från API och sparar i databasen
    public async Task<WeatherData?> FetchAndSaveAsync(double lat, double lon)
    {
        var data = await _apiClient
            .WithCoordinates(lat, lon)
            .GetWeatherAsync();

        if (data == null)
            return null;

        _context.WeatherData.Add(data);
        await _context.SaveChangesAsync();

        return data;
    }

    // Hämtar historik ur databasen
    public async Task<List<WeatherData>> GetAllAsync()
    {
        return await _context.WeatherData
            .OrderByDescending(w => w.Date)
            .ToListAsync();
    }
}
