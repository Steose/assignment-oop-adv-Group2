namespace WeatherInsight.ApiClients
{
    /// <summary>
    /// Immutable builder used to construct OpenWeather API URLs.
    /// A new instance is returned for every mutation to keep the singleton registration thread-safe.
    /// </summary>
    public class WeatherRequestBuilder
    {
        private const string BaseUrl = "https://api.openweathermap.org/data/2.5/weather";
        private readonly string _city;
        private readonly string _units;

        public WeatherRequestBuilder()
            : this(string.Empty, "metric")
        {
        }

        private WeatherRequestBuilder(string city, string units)
        {
            _city = city ?? string.Empty;
            _units = units ?? "metric";
        }

        public WeatherRequestBuilder WithCity(string city)
        {
            return new WeatherRequestBuilder(city, _units);
        }

        public WeatherRequestBuilder WithUnits(string units)
        {
            return new WeatherRequestBuilder(_city, units);
        }

        public string Build(string apiKey)
        {
            var encodedCity = Uri.EscapeDataString(_city);
            var encodedUnits = Uri.EscapeDataString(_units);
            var encodedKey = Uri.EscapeDataString(apiKey ?? string.Empty);
            return $"{BaseUrl}?q={encodedCity}&units={encodedUnits}&appid={encodedKey}";
        }
    }
}
