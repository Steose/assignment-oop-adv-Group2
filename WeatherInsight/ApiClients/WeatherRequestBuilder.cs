namespace WeatherInsight.ApiClients
{
    public class WeatherRequestBuilder
    {
        private string _baseUrl = "https://api.openweathermap.org/data/2.5/weather";
        private string _city = string.Empty;
        private string _units = "metric";

        public WeatherRequestBuilder WithCity(string city)
        {
            _city = city;
            return this;
        }

        public WeatherRequestBuilder WithUnits(string units)
        {
            _units = units;
            return this;
        }

        public string Build(string apiKey)
        {
            return $"{_baseUrl}?q={_city}&units={_units}&appid={apiKey}";
        }
    }
}
