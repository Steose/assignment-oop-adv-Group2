using Microsoft.EntityFrameworkCore;
using WeatherInsight.Models; 

namespace WeatherInsight.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<WeatherData> WeatherData => Set<WeatherData>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
    }
}
