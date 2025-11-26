# Reflektion över Grupprojekt – Advanced OOP

**Namn:** Hanita Melin  
**Grupp:** WeatherInsight  
**Datum:** 26/11/2025  
**Antal personer i gruppen:** 3

---

## **Del 1: Grundreflektion (G)**

### 1. Projektöversikt och min roll

Projektet _WeatherInsight_ syftade till att skapa en applikation som hämtar, lagrar och presenterar väderdata från ett externt API. Applikationen använder **ASP.NET Core**, **Entity Framework Core**, **SQLite**, och följer **MVC-arkitektur** med separerade lager för presentation, affärslogik och datalagring.

Min primära roll var **backendutvecklare** i projektets första fas. Jag ansvarade för att skapa **modell- och datalagret**, implementera **EF Core**, definiera **DbContext**, bygga grundstrukturen för **Repository- och Service-lager**, samt utveckla **WeatherApiClient** för API-anrop. Jag arbetade inte med frontend, caching, eller initial API-nyckelhantering.

---

### 2. Teknisk implementation

Dataflödet i applikationen fungerar så här: `WeatherApiClient` skickar HTTP-förfrågningar till ett väder-API och deserialiserar JSON-svar till `WeatherData`-objekt. Dessa objekt kan sedan sparas via **WeatherRepository** i EF Core-databasen.  
Service-lagret, **WeatherService**, ansvarar för affärslogik som historikbegränsning (sista 10 posterna) och sortering inför presentation.

När data hämtas från API sker det via `HttpClient` och DI hanterar livscykeln. Databasen uppdateras med `DbSet<WeatherData>` och automatisk migration sker vid applikationsstart, vilket säkerställer att systemet fungerar på **Windows, Mac och Linux**.

Caching implementerades inte av mig, utan var planerat för senare. Detta innebär att alla anrop från API går direkt till servern och sparas i EF Core innan de används av presentationen.

Vid CRUD och normalisering stämde vi av med kursledaren att en historikdatabas inte behöver dessa. 

---

### 3. Design Patterns

**Pattern 1: Builder**

- **Problem:** API-anrop kräver flexibel URL-konstruktion med varierande parametrar (stad, enheter, API-nyckel). Hårdkodade URL:er blir svåra att underhålla och testa.
- **Lösning:** `WeatherRequestBuilder` bygger URL:en steg för steg med fluent interface. Detta separerar URL-logik från API-klienten, gör koden testbar och lätt att utöka med nya parametrar.
- **Kodexempel:**

```csharp
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

    public string Build(string apiKey)
    {
        var encodedCity = Uri.EscapeDataString(_city);
        var encodedUnits = Uri.EscapeDataString(_units);
        var encodedKey = Uri.EscapeDataString(apiKey ?? string.Empty);
        return $"{_baseUrl}?q={encodedCity}&units={encodedUnits}&appid={encodedKey}";
    }
}
```

**Pattern 2: Repository**

- **Problem:** Direkt databasåtkomst i controllers/services skapar tight coupling till EF Core och gör enhetstestning omöjlig. Affärslogik blandas med datalagring.
- **Lösning:** `WeatherRepository` kapslar in all EF Core-logik bakom interface `IWeatherRepository`. Detta möjliggör att mocka databasen i tester och byta databas-implementation utan att påverka övrig kod.
- **Kodexempel:**

```csharp
public class WeatherRepository : IWeatherRepository
{
    private readonly AppDbContext _context;

    public WeatherRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<WeatherData?> GetAsync(string city)
    {
        return await _context.WeatherData
            .FirstOrDefaultAsync(x => x.City.ToLower() == city.ToLower());
    }

    public async Task SaveAsync(WeatherData data)
    {
        _context.WeatherData.Add(data);
        await _context.SaveChangesAsync();
    }
}
```

**Pattern 3: MVC (Model-View-Controller)**

- **Problem:** Blandning av affärslogik och datahantering skapar spagettikod som är svår att underhålla, testa och vidareutveckla.
- **Lösning:** MVC separerar ansvar i tre lager: **Model** (WeatherData) representerar data, **Controller** (HomeController) hanterar requests och orchestrerar tjänster, **View** (Razor) ansvarar för presentation. Detta ger tydlig separation of concerns.
- **Kodexempel:**

```csharp
// Model - representerar väderdata
public class WeatherData
{
    public int Id { get; set; }
    public string City { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public string Condition { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

// Controller - hanterar requests
public class HomeController : Controller
{
    private readonly WeatherService _weatherService;

    public async Task<IActionResult> Index(string city = "Stockholm")
    {
        var latest = await _weatherService.FetchAndSaveAsync(city);
        var history = await _weatherService.GetAllAsync();
        
        ViewData["LatestWeather"] = latest;
        ViewData["History"] = history;
        return View();
    }
}
```

**Pattern 4: Dependency Injection (DI)**

- **Problem:** Hårdkodade beroenden (new HttpClient(), new DbContext()) skapar tight coupling, gör testning svår och hindrar konfiguration per miljö.
- **Lösning:** ASP.NET Core's DI container injicerar beroenden via konstruktorer. Detta ger flexibilitet, testbarhet och centraliserad konfiguration i `Program.cs`.
- **Kodexempel:**

```csharp
// I Program.cs - registrering
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=weather.db"));
builder.Services.AddScoped<IWeatherRepository, WeatherRepository>();
builder.Services.AddScoped<WeatherService>();

// I WeatherService - injection
public class WeatherService
{
    private readonly AppDbContext _context;
    private readonly IWeatherApiClient _apiClient;
    private readonly IMemoryCache _cache;

    public WeatherService(AppDbContext context, IWeatherApiClient apiClient, IMemoryCache cache)
    {
        _context = context;
        _apiClient = apiClient;
        _cache = cache;
    }
}
```

---

### **Punkt 8 - Cache-strategi 

Caching implementerades av en klasskamrat.

Projektet använder **IMemoryCache** från ASP.NET Core för att cacha API-svar i 15 minuter (TTL). När `WeatherApiClient.FetchWeatherAsync()` anropas:

1. **Cache hit:** Om stad finns i cache (`_cache.TryGetValue(cacheKey, out WeatherData? cachedWeatherData)`) returneras cachad data direkt, Id sätts till 0 för att undvika databaskonflikt.
2. **Cache miss:** API anropas via HttpClient, JSON deserialiseras, och resultatet sparas i cache (`_cache.Set(cacheKey, newWeatherData, TimeSpan.FromMinutes(15))`).

Detta minskar API-anrop drastiskt för populära städer. Async/await används konsekvent för icke-blockerande I/O. Retry-policy (Polly) med 3 försök och circuit breaker (efter 3 fel, 10s paus) hanterar API-instabilitet.

---

### 4. Utmaningar

- **Plattformskompatibla migrationer:** SQLite fungerade olika på Windows/Mac/Linux → löstes med automatisk migrering i `Program.cs`. Jag implementerade detta. 
    
- **Flexibel API-struktur:** Byta API skulle bli krångligt → löstes med Builder-pattern och ren service-struktur.
    
- **Samtidiga anrop:** Risk för socket exhaustion → DI hanterar `HttpClient`. Jag implementerade detta. 
    
- **Historikbegränsning:** Växande databas → hanteras i Service-lagret med begränsning till 10 poster. Jag implementerade detta. 
    

Nästa gång skulle jag tidigt definiera **plattformstest och CI/CD** för att undvika migrationsproblem.

---

### 5. Lärdomar

- Arkitektur blir stark med tydlig separation av ansvar.
    
- Builder-, Repository- och MVC-mönster gör koden testbar och modulär.
    
- EF Core + SQLite ger enkel, skalbar lösning över plattformar.
    
- Separering mellan API-klient, service och repository underlättar vidareutveckling.
    
- Viktigt att planera för plattformsoberoende från start.
    
- Samarbete i gruppen kräver tydlig ansvarsfördelning och tydlig kommunikation.
    

---

### 6. Grupparbetet

- **Medlemmar:** [Claes och Stephen]
    
- **Samarbete:** Gruppen fungerade harmoniskt med hög kreativitet. Konflikter var få, främst kring API-struktur och lösning av olika utmaningar som uppstod, och löstes genom diskussion.
    

---

## **Del 2: Fördjupning (VG)**

### 7. Arkitektur och kodkvalitet

Koden är strukturerad i lager: **Presentation (Controllers/Views)**, **Business (WeatherService)**, **Data (Repository/DbContext/Models)**. DI används för HttpClient, repository och service, vilket gör komponenter testbara isolerat. Exempelvis kan `WeatherRepository` testas med en in-memory DB utan att påverka Controllers. Tight coupling undviks genom interface-baserad design och tydlig separation mellan lager.

---

### 8. Cache-strategi och prestanda

Caching implementerades av en klasskamrat. 

---

### 9. GitFlow och versionshantering

Vi använde GitFlow: `main` för stabil release, `develop` för pågående utveckling och `feature branches` för nya funktioner. Merge och konfliktlösning skedde via pull requests. GitFlow gav struktur, undvek att alla committar direkt till main och gjorde det lätt att isolera och testa nya funktioner innan integration.

---

### 10. Design-beslut och alternativ

- **Val av EF Core + SQLite över MongoDB:**
    
    - **Fördelar:** Plattformoberoende, enkel migrering, stark typkontroll, låg konfigurationskostnad.
        
    - **Nackdelar:** Mindre flexibelt för schemalösa dokument.
        
- **Alternativ:** MongoDB hade krävt annan abstraktion och kunde ge prestandafördelar vid stora datamängder, men EF Core gav enkel integration och migrationsstöd.
    

---

### 11. Vidareutveckling

1. **Fullständig Azure-Key Vault-integration:** Säker API-nyckelhantering för alla miljöer.
    
2. **Unit testing:** Testa service- och repository-lager med mockade API-anrop och in-memory DB.
    
3. **Strategi för flera API-källor:** Fortsätta med Strategy-pattern för fler vädertjänster.
    
4. **Frontendförbättringar:** Koppla MVC-lagren till responsiv UI med live-data.
    

---

### 12. Arbetsprocess och verktyg

- **Verktyg:** Visual Studio, GitHub, Trello, Drawio, EF Core dokumentation, kursmaterial, diskussioner med kollegor.
    
- **AI-användning:** ChatGPT användes för förklaring av API-Key implementering, förklaring av designmönster, felhantering och felsökning. All information verifierades mot den faktiska koden och projektets implementationsval.
    

---

### **Kodbilaga**

### **Exempel 1: Builder Pattern i WeatherRequestBuilder**


```csharp
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

    public string Build(string apiKey)
    {
        var encodedCity = Uri.EscapeDataString(_city);
        return $"{_baseUrl}?q={encodedCity}&units={_units}&appid={apiKey}";
    }
}
```

### **Exempel 2: Repository Pattern med EF Core**


```csharp
public class WeatherRepository : IWeatherRepository
{
    private readonly AppDbContext _context;

    public async Task SaveAsync(WeatherData data)
    {
        _context.WeatherData.Add(data);
        await _context.SaveChangesAsync();
    }

    public async Task<WeatherData?> GetAsync(string city)
    {
        return await _context.WeatherData
            .FirstOrDefaultAsync(x => x.City.ToLower() == city.ToLower());
    }
}
```

### **Exempel 3: MVC Pattern - Model och Controller**


```csharp
// Model
public class WeatherData
{
    public int Id { get; set; }
    public string City { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public string Condition { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

// Controller
public class HomeController : Controller
{
    private readonly WeatherService _weatherService;

    public async Task<IActionResult> Index(string city = "Stockholm")
    {
        var latest = await _weatherService.FetchAndSaveAsync(city);
        ViewData["LatestWeather"] = latest;
        return View(); // View renderas med data
    }
}
```


### **Exempel 4: Caching i WeatherApiClient**


```csharp
public async Task<WeatherData?> FetchWeatherAsync(string city)
{
    var cacheKey = $"weatherdata_{city}";
    
    // Cache hit - returnera cachad data
    if (_cache.TryGetValue(cacheKey, out WeatherData? cachedWeatherData))
    {
        cachedWeatherData.Id = 0;
        return cachedWeatherData;
    }

    // Cache miss - hämta från API
    var json = await _client.GetStringAsync(_builder.WithCity(city).Build(_apiKey));
    var newWeatherData = /* deserialisering */;
    
    _cache.Set(cacheKey, newWeatherData, TimeSpan.FromMinutes(15));
    return newWeatherData;
}
```

---



