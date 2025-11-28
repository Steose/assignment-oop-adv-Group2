### **WeatherInsight API Documentation**

#### **Overview architecture**

The application follows the MVC (Model-View-Controller) design pattern, where the Controller layer handles the API communication.

- `HomeController` handles HTTP-requests
- `WeatherService` coordinates external API + database API
- `WeatherRepository` encapsulates EF Core-operations
- `WeatherApiClient` communicates with external OpenWeather API

#### **Endpoints**

**GET /**  
**GET /?city={cityName}**

- **Description:** Gets weatherdata for selected city and shows history
- **Parameters:**
    - `city` (query, optional): Name of city (default: "Stockholm")
- **Response:** HTML-view with current weather + 10 latest searches.
- **Error handling:** In case of non valid city name, an error message is dispalyed in ViewData["City name is wrong"]

#### **Data flow**

1. **User Requests**

```csharp
   var latest = await _weatherService.FetchAndSaveAsync(city);
   var history = await _weatherService.GetAllAsync();
```

2. **WeatherService.FetchAndSaveAsync(city)**
    - Calls `_apiClient.FetchWeatherAsync(city)`
    - Saves results with `_context.WeatherData.Add(data)`
    - Returns `WeatherData`-object
3. **WeatherApiClient.FetchWeatherAsync(city)**
    - **Cache check:** `_apiCacheService.TryGetWeatherFromCache(city, out WeatherData? cachedWeatherData)`
    - **API call (cache miss):**


```csharp
     var url = _builder.WithCity(city).Build(_apiKey);
     var json = await _client.GetStringAsync(url);
```

- **Deserialization:** JSON → `WeatherData`-object
- **Cache update:** `_apiCacheService.CacheWeatherData(city, newWeatherData)`

4. **Database persistence (EF Core)**
    - `AppDbContext.WeatherData.Add(data)`
    - `SaveChangesAsync()` writes to SQLite (`weather.db`)

#### **Models**

**WeatherData**


```csharp
public class WeatherData
{
    public int Id { get; set; }              // Primary key (auto-increment)
    public string City { get; set; }         // Cityname
    public double Temperature { get; set; }  // Temperature (°C)
    public string Condition { get; set; }    // Weather description
    public DateTime Date { get; set; }       // Search timestamp
}
```

#### **Caching strategy**

- **Type:** In-memory cache (IMemoryCache)
- **TTL:** 15 minutes
- **Cache key:** `"weatherdata_{city}"`
- **Invalidation:** Automatic after TTL, no manual invalidation
- **Cache hit:** Returns data directly, sets `Id = 0` to prevent Primary Key conflict
- **Cache miss:** API-call → deserialization → cache + database

#### **Error handling**

- **404 Not Found (OpenWeather API):** Throws `ArgumentException("City name is wrong")`
- **HTTP-error:** Polly retry-policy (3 tries, exponential backoff)
- **Circuit breaker:** After 3 failed calls, 10 seconds pause
- **Timeout:** 10 seconds per request (`client.Timeout`)

#### **Säkerhet**

- **API-nyckel:** Läses från miljövariabel `OPENWEATHER_API_KEY`
- **Security headers:** `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`
- **HTTPS:** Tvingande omdirigering i produktion

#### **Databasmigration**

Automatisk migration vid appstart via `Program.cs`:


```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}
```