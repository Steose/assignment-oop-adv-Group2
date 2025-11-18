using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WeatherInsight.Models;
using WeatherInsight.Services;

namespace WeatherInsight.Controllers
{
    /// <summary>
    /// Handle user requests for the landing pages and orchestrate weather retrieval.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly WeatherService _weatherService;

        public HomeController(ILogger<HomeController> logger, WeatherService weatherService)
        {
            _logger = logger;
            _weatherService = weatherService;
        }

        /// <summary>
        /// Render the home page, optionally fetching weather for the supplied city.
        /// </summary>
        public async Task<IActionResult> Index(string? city)
        {
            var normalizedCity = string.IsNullOrWhiteSpace(city) ? null : city.Trim();
            WeatherData? latest = null;

            if (normalizedCity is not null)
            {
                latest = await _weatherService.FetchAndSaveAsync(normalizedCity);
                if (latest == null)
                {
                    ViewBag.ErrorMessage =
                        "City not found. Please check the spelling.";
                    _logger.LogWarning("Failed to fetch weather for {City}", normalizedCity);
                }
            }

            var history = await _weatherService.GetAllAsync();

            // Fall back to the newest stored entry if we could not fetch fresh data.
            latest ??= history.FirstOrDefault();

            ViewData["SelectedCity"] = normalizedCity ?? "Stockholm";
            ViewData["LatestWeather"] = latest;
            ViewData["History"] = history;

            return View();
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
