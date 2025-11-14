using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WeatherInsight.Models; 

namespace WeatherInsight.Data
{
    public class CacheDbContext : DbContext
    {
        //public DbSet<CachedWeatherData> WeatherData { get; set; }
        public DbSet<CachedWeatherData> CachedWeatherData => Set<CachedWeatherData>();

        public CacheDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
    }
}