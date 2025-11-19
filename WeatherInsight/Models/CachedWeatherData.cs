using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeatherInsight.Models
{
    // Source: https://github.com/Campus-Molndal-CLO25/Studiematerial/blob/main/3_advanced_oop/lectures/caching/caching_basics_marp.md#mongodb-cache-
    public class CachedWeatherData
    {
        public int Id { get; set; }
        public string City { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public string Condition { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }
}