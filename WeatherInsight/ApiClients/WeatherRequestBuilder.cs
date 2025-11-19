namespace WeatherInsight.ApiClients
{
    public class WeatherRequestBuilder
    {
        private string _baseUrl = "https://api.openweathermap.org/data/2.5/weather";
        private string _city = string.Empty;
        private string _units = "metric";

        public WeatherRequestBuilder WithCity(string city)
        {
            _city = city ?? string.Empty;
            return this;
        }

        public WeatherRequestBuilder WithUnits(string units)
        {
            _units = units ?? "metric";
            return this;
        }

        public string Build(string apiKey)
        {
            var encodedCity = Uri.EscapeDataString(_city);
            var encodedUnits = Uri.EscapeDataString(_units);
            var encodedKey = Uri.EscapeDataString(apiKey ?? string.Empty);
            return $"{_baseUrl}?q={encodedCity}&units={encodedUnits}&appid={encodedKey}";
        }
    }
}
