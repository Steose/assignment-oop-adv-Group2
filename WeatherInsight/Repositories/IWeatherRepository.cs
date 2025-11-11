using WeatherInsight.Models;

namespace WeatherInsight.Repositories
{
    public interface IWeatherRepository
    {
        Task<WeatherData?> GetAsync(string city);
        Task SaveAsync(WeatherData data);
    }
}
