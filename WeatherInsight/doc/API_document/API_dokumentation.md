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

#### **Modeller**

**WeatherData**


```csharp
public class WeatherData
{
    public int Id { get; set; }              // Primary key (auto-increment)
    public string City { get; set; }         // Stadsnamn
    public double Temperature { get; set; }  // Temperatur (°C)
    public string Condition { get; set; }    // Väderbeskrivning
    public DateTime Date { get; set; }       // Tidsstämpel för sökning
}
```

#### **Caching-strategi**

- **Type:** In-memory cache (IMemoryCache)
- **TTL:** 15 minuter
- **Cache key:** `"weatherdata_{city}"`
- **Invalidering:** Automatisk efter TTL, ingen manuell invalidering
- **Cache hit:** Returnerar data direkt, sätter `Id = 0` för att undvika PK-konflikt
- **Cache miss:** API-anrop → deserialisering → cache + databas

#### **Felhantering**

- **404 Not Found (OpenWeather API):** Kastas som `ArgumentException("City name is wrong")`
- **HTTP-fel:** Polly retry-policy (3 försök, exponentiell backoff)
- **Circuit breaker:** Efter 3 misslyckade anrop, 10 sekunders paus
- **Timeout:** 10 sekunder per request (`client.Timeout`)

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