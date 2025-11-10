namespace WeatherInsight.Models
{
    public class WeatherData
    {
        public int Id { get; set; }
        public string City { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public string Condition { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }
}
