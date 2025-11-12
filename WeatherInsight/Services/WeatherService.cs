using Microsoft.EntityFrameworkCore;
using WeatherInsight.ApiClients;
using WeatherInsight.Data;
using WeatherInsight.Models;

namespace WeatherInsight.Services;

/// <summary>
/// Coordinates API calls and database persistence for weather data.
/// </summary>
public class WeatherService
{
    // Step 1: Cache the EF Core DbContext so we can write/read weather rows.
    private readonly AppDbContext _context;

    // Step 2: Cache the HTTP API client that knows how to reach OpenWeather.
    private readonly IWeatherApiClient _apiClient;

    public WeatherService(AppDbContext context, IWeatherApiClient apiClient)
    {
        // Step 3: Capture the injected DbContext instance coming from DI.
        _context = context;

        // Step 4: Capture the injected API client so we can make HTTP calls later.
        _apiClient = apiClient;
    }

    /// <summary>
    /// Step 5: Fetch the latest weather for the requested city and store it.
    /// </summary>
    public async Task<WeatherData?> FetchAndSaveAsync(string city)
    {
        // Step 6: Ask the API for the current weather snapshot.
        var data = await _apiClient.FetchWeatherAsync(city);

        // Step 7: Abort early if the API could not provide data.
        if (data == null)
        {
            return null;
        }

        // Step 8: Track the new entity so EF Core will insert it.
        _context.WeatherData.Add(data);

        // Step 9: Persist the pending insert asynchronously.
        await _context.SaveChangesAsync();

        // Step 10: Return the saved entity to the caller.
        return data;
    }

    /// <summary>
    /// Step 11: Return all saved weather entries ordered from newest to oldest.
    /// </summary>
    public async Task<List<WeatherData>> GetAllAsync()
    {
        // Step 12: Build and execute the query that retrieves the history.
        return await _context.WeatherData
            .OrderByDescending(w => w.Date)
            .ToListAsync();
    }
}
