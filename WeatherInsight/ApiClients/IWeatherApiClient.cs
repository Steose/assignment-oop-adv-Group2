using WeatherInsight.Models;

namespace WeatherInsight.ApiClients
{
    public interface IWeatherApiClient
    {
        Task<WeatherData?> FetchWeatherAsync(string city);
    }
}
