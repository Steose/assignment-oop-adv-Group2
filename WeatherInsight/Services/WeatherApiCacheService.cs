using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using WeatherInsight.Models;

namespace WeatherInsight.Services
{
    // With help from Github/Claude Haiku 4.5, https://github.com/copilot/share/423d5034-08a4-8422-8040-a84a84c64835
    public class WeatherApiCacheService : IWeatherApiCacheService
    {
        private readonly IMemoryCache _cache;
        private const int CACHE_TIME_TO_LIVE_MINUTES = 15;
        private const string CACHE_KEY_PREFIX = "weatherdata_";

        public WeatherApiCacheService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public bool TryGetWeatherFromCache(string city, out WeatherData? cachedWeatherData)
        {
            var cacheKey = GenerateCacheKey(city);
            
            if (_cache.TryGetValue(cacheKey, out WeatherData?  weatherData))
            {
                // The program expects the Id of new searches to be 0.
                // The Id of the cached search is already taken in the search history weather.db database.
                weatherData.Id = 0;
                cachedWeatherData = weatherData;
                return true;
            }

            cachedWeatherData = null;
            return false;
        }

        public void CacheWeatherData(string city, WeatherData weatherData)
        {
            var cacheKey = GenerateCacheKey(city);
            _cache.Set(cacheKey, weatherData, TimeSpan.FromMinutes(CACHE_TIME_TO_LIVE_MINUTES));
        }

        private string GenerateCacheKey(string city)
        {
            return $"{CACHE_KEY_PREFIX}{city}";
        }
    }
}