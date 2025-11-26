### **WeatherInsight API Documentation**

#### **Arkitekturöversikt**

Applikationen följer en lagerad arkitektur:

- **Presentation:** `HomeController` hanterar HTTP-requests
- **Business Logic:** `WeatherService` koordinerar API + databas
- **Data Access:** `WeatherRepository` kapslar EF Core-operationer
- **External API:** `WeatherApiClient` kommunicerar med OpenWeather API

#### **Endpoints**

**GET /**  
**GET /?city={cityName}**

- **Beskrivning:** Hämtar väderdata för angiven stad och visar historik
- **Parameters:**
    - `city` (query, optional): Stadsnamn (default: "Stockholm")
- **Response:** HTML-vy med aktuellt väder + 10 senaste sökningar
- **Felhantering:** Vid ogiltig stad visas felmeddelande i ViewData["Error"]

#### **Dataflöde**

1. **User Request → HomeController.Index(city)**



```csharp
   var latest = await _weatherService.FetchAndSaveAsync(city);
   var history = await _weatherService.GetAllAsync();
```

2. **WeatherService.FetchAndSaveAsync(city)**
    - Anropar `_apiClient.FetchWeatherAsync(city)`
    - Sparar resultat via `_context.WeatherData.Add(data)`
    - Returnerar `WeatherData`-objekt
3. **WeatherApiClient.FetchWeatherAsync(city)**
    - **Cache check:** `_cache.TryGetValue(cacheKey, out WeatherData? cachedData)`
    - **API call (cache miss):**



```csharp
     var url = _builder.WithCity(city).Build(_apiKey);
     var json = await _client.GetStringAsync(url);
```

- **Deserialisering:** JSON → `WeatherData`-objekt
- **Cache update:** `_cache.Set(cacheKey, data, TimeSpan.FromMinutes(15))`

4. **Database persistence (EF Core)**
    - `AppDbContext.WeatherData.Add(data)`
    - `SaveChangesAsync()` skriver till SQLite (`weather.db`)

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