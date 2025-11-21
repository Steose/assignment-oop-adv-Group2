using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using WeatherInsight.Models;
using WeatherInsight.Services;

namespace WeatherInsight.Controllers
{
    /// <summary>
    /// Step 1: Handle user requests for the landing pages and orchestrate weather retrieval.
    /// </summary>
    public class HomeController : Controller
    {
        // Step 2: Hold onto the logger so we can emit diagnostics if needed.
        private readonly ILogger<HomeController> _logger;

        // Cache for the wather api, source: https://github.com/Campus-Molndal-CLO25/Studiematerial/blob/main/3_advanced_oop/lectures/caching/caching_basics_marp.md
        private readonly IMemoryCache _cache;

        // Step 3: Hold onto the WeatherService so actions can fetch/persist weather data.
        private readonly WeatherService _weatherService;

        public HomeController(ILogger<HomeController> logger, WeatherService weatherService, IMemoryCache cache)
        {
            // Step 4: Capture the injected logger from ASP.NET Core's DI container.
            _logger = logger;
            _cache = cache;

            // Step 5: Capture the injected WeatherService registered earlier in Program.cs.
            _weatherService = weatherService;
        }

        /// <summary>
        /// Step 6: Render the home page, optionally fetching weather for the supplied city.
        /// </summary>
        public async Task<IActionResult> Index(string city = "Stockholm")
        {
            // Step 7: Call the service to fetch + save the latest snapshot for the requested city.
            WeatherData? latest = null;
            try
            {
                latest = await _weatherService.FetchAndSaveAsync(city);
            }
            catch (ArgumentException ex) when (ex.Message == "City name is wrong")
            {
                _logger.LogWarning(ex, "Invalid city input: {City}", city);
                ViewData["Error"] = ex.Message;
            }

            // Step 8: Load the stored history so the view can show recent lookups.
            var history = await _weatherService.GetAllAsync();

            // Step 9: Package the data for the Razor view using ViewData for quick prototyping.
            ViewData["SelectedCity"] = city;
            ViewData["LatestWeather"] = latest;
            ViewData["History"] = history;

            // Step 10: Render the Index.cshtml view with the populated ViewData payload.
            return View();
        }

        /// <summary>
        /// Step 11: Serve the privacy page (static content in this sample).
        /// </summary>
        public IActionResult Privacy()
        {
            // Step 12: The view already contains the static message, so just render it.
            return View();
        }

        /// <summary>
        /// Step 13: Render the standard error page with diagnostic information.
        /// </summary>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // Step 14: Provide the request identifier so users/devs can correlate logs.
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}