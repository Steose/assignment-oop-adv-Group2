using Microsoft.EntityFrameworkCore;
using WeatherInsight.Data;
using WeatherInsight.Models;

namespace WeatherInsight.Repositories
{
    public class WeatherRepository : IWeatherRepository
    {
        private readonly AppDbContext _context;

        public WeatherRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<WeatherData?> GetAsync(string city)
        {
            return await _context.WeatherData
                .FirstOrDefaultAsync(x => x.City.ToLower() == city.ToLower());
        }

        public async Task SaveAsync(WeatherData data)
        {
            _context.WeatherData.Add(data);
            await _context.SaveChangesAsync();
        }
    }
}
