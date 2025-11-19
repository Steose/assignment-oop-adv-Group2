using Microsoft.EntityFrameworkCore;
using WeatherInsight.ApiClients;
using WeatherInsight.Data;
using WeatherInsight.Models;

namespace WeatherInsight.Services
{
    /// <summary>
    /// Coordinates API calls and database persistence for weather data.
    /// </summary>
    public class WeatherService
    {
        private readonly AppDbContext _context;
        private readonly IWeatherApiClient _apiClient;

        public WeatherService(AppDbContext context, IWeatherApiClient apiClient)
        {
            _context = context;
            _apiClient = apiClient;
        }

        /// <summary>
        /// Fetch the latest weather for the requested city and store it.
        /// </summary>
        public async Task<WeatherData?> FetchAndSaveAsync(string city)
        {
            if (string.IsNullOrWhiteSpace(city))
            {
                return null;
            }

            var data = await _apiClient.FetchWeatherAsync(city);

            if (data == null)
            {
                return null;
            }

            _context.WeatherData.Add(data);
            await _context.SaveChangesAsync();

            return data;
        }

        /// <summary>
        /// Return all saved weather entries ordered from newest to oldest.
        /// </summary>
        public async Task<List<WeatherData>> GetAllAsync()
        {
            return await _context.WeatherData
                .OrderByDescending(w => w.Date)
                .ToListAsync();
        }

        /// <summary>
        /// Return the most recent weather entry if one exists.
        /// </summary>
        public async Task<WeatherData?> GetLatestAsync()
        {
            return await _context.WeatherData
                .OrderByDescending(w => w.Date)
                .FirstOrDefaultAsync();
        }
    }
}
