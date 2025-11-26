using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WeatherInsight.Models;

namespace WeatherInsight.Services
{
    public interface IWeatherApiCacheService
    {
        bool TryGetWeatherFromCache(string city, out WeatherData?  cachedWeatherData);
        void CacheWeatherData(string city, WeatherData weatherData);
    }
}