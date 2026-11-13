# Reflektion över Grupprojekt - Advanced OOP

**Namn:** Stephen Osedumme  
**Grupp:** WeatherInsight (G2)  
**Datum:** 2025-11-26  
**Antal personer i gruppen** 3

---

## Del 1: Grundreflektion (G)

### 1. Projektöversikt och min roll

**Beskriv projektets syfte och vilken roll du hade i gruppen.**


Projektet _WeatherInsight_ är en ASP.NET Core MVC-applikation som hämtar väderdata från OpenWeather, sparar resultat i en SQLite-databas via Entity Framework Core och visar senaste sökning samt historik i UI. Vi använde C#, EF Core, Razor Views, HttpClient, IMemoryCache och DI. Min huvudroll var att stabilisera integrationsdelen mellan API och presentationen: jag kopplade om dependency injection så att WeatherService, WeatherApiClient och WeatherRequestBuilder injiceras korrekt, förbättrade felhantering/loggning när API-nyckel saknas eller stad är fel, och byggde om startsidan så att senaste vädret, historiken och val av stad visas tydligt. Jag tog även ansvar för dokumentation och struktur, så att flödet från Controller - Service - API/DB blev lätt att förstå.

---

### 2. Teknisk implementation


Dataflödet börjar i `HomeController.Index`, som anropar `WeatherService.FetchAndSaveAsync(city)`. Service-lagret använder en typad `IWeatherApiClient` för att hämta data. I `WeatherApiClient.FetchWeatherAsync` byggs URL:en via `WeatherRequestBuilder`, och innan ett API-anrop görs kontrolleras `IMemoryCache` med nyckeln `weatherdata_{city}`. Vid cache hit returneras objektet direkt och `Id` sätts till 0 för att undvika konflikt när EF Core sparar ny historikpost. Vid cache miss hämtas JSON från API, deserialiseras till `WeatherData`, cachas i 15 minuter (TTL) och skickas tillbaka till service-lagret. Därefter sparar `WeatherService` objektet i EF Core med `_context.WeatherData.Add(data)` och `SaveChangesAsync()`. Historiken hämtas sedan via `GetAllAsync()` som sorterar på `Date` och begränsar till de 10 senaste posterna. På så sätt används cache för snabba återanrop, EF för persistens och API:t som primär källa.

---

### 3. Design Patterns


**Pattern 1:**
Builder-pattern användes för att konstruera API-URL:er på ett säkert och utbyggbart sätt. Problemet var att hårdkodade strängar snabbt blir svåra att underhålla när parametrar som stad och enheter varierar. `WeatherRequestBuilder` kapslar uppbyggnaden och gör koden testbar samt tydlig. Exempel: `WithCity(city)` och `Build(apiKey)` skapar den slutliga URL:en utan att API-klienten behöver känna till formatet. Det minskade duplication och gjorde det enkelt att lägga till fler parametrar senare.

**Pattern 2:**
Repository-pattern kapslar EF Core-logik i `WeatherRepository` bakom ett interface. Problemet är att direkt databasåtkomst i controllers/services skapar tight coupling och gör testning svår. Repository-lagret ger en tydlig kontrakt-yta (`GetAsync`, `SaveAsync`) som kan mockas i tester. I vår kod går all datalagring genom `AppDbContext` via repositoryn, vilket gör det lättare att byta databas eller testa isolerat.

**Pattern 3:**
Dependency Injection (DI) används konsekvent i hela applikationen. Problemet är att manuella instanser (`new HttpClient()`, `new DbContext()`) ger rigid kod och risk för resursläckor. Genom DI i `Program.cs` registreras `AppDbContext`, `WeatherService`, `WeatherApiClient` och `WeatherRequestBuilder`. Detta gör att livscykler hanteras centralt, att services kan testas isolerat och att konfiguration (t.ex. HttpClient-timeouts och Polly-policies) hålls samlad.

---

### 4. Utmaningar


Den svåraste utmaningen var att få API-integration, cache och EF-persistens att fungera stabilt i samma flöde. Vi fick återkommande fel när API-nyckeln saknades eller när staden var felstavad, vilket ledde till krasch och tom UI. Jag löste det genom tydligare felhantering i `WeatherApiClient` (t.ex. hantering av 404 och validering av API-nyckel), loggning via `ILogger` i `HomeController`, och genom att uppdatera vyn så att felmeddelanden och senaste data visas tydligt. En annan del var cache-objektens `Id`, som orsakade konflikt när samma objekt sparades i databasen; därför nollställs `Id` vid cache hit. Nästa gång skulle jag lägga till automatiska integrationstester och köra dem tidigt, samt införa en tydligare felbanner i UI och en mer central felhanteringsstrategi för API-anrop.

---

### 5. Lärdomar


Det viktigaste jag lärt mig är hur avgörande tydlig arkitektur och ansvarsfördelning är för stabilitet och underhållbarhet. Tekniskt stärkte jag min förståelse för hur EF Core, HttpClient och IMemoryCache samverkar i en real-time applikation, och varför patterns som DI, Builder och Repository gör koden testbar och flexibel. Jag lärde mig också vikten av att hantera fel tidigt och ge användaren tydlig feedback. Mjukare lärdomar var att planera arbetsuppgifter tydligt, att dokumentation sparar mycket tid för hela gruppen, och att snabba avstämningar minskar missförstånd kring ansvar och leveranser.


### 6. Grupparbetet


a) Hanita Melin, Claes.  
b) Samarbetet fungerade överlag bra med tydlig ansvarsfördelning. Vi hade löpande avstämningar om API-integration, datalagring och UI. När konflikter uppstod handlade de mest om prioritering av funktioner (t.ex. cache vs. UI-förbättringar) och löstes genom gemensamma beslut och snabb prototyping.

---

## Del 2: Fördjupning (VG)


### 7. Arkitektur och kodkvalitet


Koden är strukturerad i tre lager: presentation (Controllers/Views), business (WeatherService) och data (Repository/DbContext/Models). Controllers innehåller ingen affärslogik, utan orchestrerar anrop och mappar data till ViewData. Business-lagret ansvarar för flödet API - cache - DB samt historiklogik (ordning och begränsning till 10 rader). Data-lagret kapslar EF Core i `AppDbContext` och `WeatherRepository`. DI används för att injicera HttpClient, DbContext, repository och service, vilket minskar tight coupling och gör komponenter testbara isolerat. Exempelvis kan `WeatherService` testas med en mockad `IWeatherApiClient` och en in-memory `AppDbContext`, utan att API eller UI behövs. Genom att använda interfaces (t.ex. `IWeatherApiClient` och `IWeatherRepository`) kan vi ersätta implementationer vid testning eller framtida utveckling, vilket ökar kodkvalitet och flexibilitet.

---

### 8. Cache-strategi och prestanda

**Hur implementerade ni caching-lagret och vilka prestanda-överväganden gjorde ni?**


Caching implementerades med `IMemoryCache` i `WeatherApiClient`. Varje stad får en cache-nyckel (`weatherdata_{city}`) och data lagras med en TTL på 15 minuter. En cache hit betyder att värdet finns i minnet och kan returneras direkt utan API-anrop; då nollställs `Id` så att EF Core kan spara en ny historikrad. En cache miss innebär att data saknas och att klienten måste anropa OpenWeather, deserialisera JSON och därefter skriva till cache. Async/await används för alla nätverks- och databasoperationer, vilket gör att UI-tråden inte blockeras och att applikationen skalar bättre under flera samtidiga anrop. Om jag byggde om lösningen skulle jag flytta cachelogiken till service-lagret för tydligare separation, införa sliding expiration för aktiva städer, lägga till ett distribuerat cache-alternativ (t.ex. Redis) för framtida skalning, samt centralisera invalidation baserat på tidsstämplar i databasen.

---

### 9. GitFlow och versionshantering


Vi använde en förenklad GitFlow-modell: `main` innehöll stabila versioner, `develop` var integrationsbranch och varje större ändring gjordes i en feature-branch (t.ex. `feature/cache` eller `feature/ui`). När en feature var klar gjordes en pull request till `develop`, där vi granskade koden och löste konflikter innan merge. Fördelen är att `main` aldrig bryts av ofärdiga ändringar, att flera personer kan arbeta parallellt utan att blockera varandra, och att konflikter fångas tidigt i integrationsfasen. GitFlow är bättre än att committa direkt till `main` eftersom det ger kontroll, möjlighet till kodgranskning och en tydlig release-historik.

---

### 10. Design-beslut och alternativ


Ett centralt designbeslut var att använda `IMemoryCache` istället för en distribuerad cache som Redis. Fördelarna med IMemoryCache är enkel implementation, ingen extern infrastruktur och minimal konfiguration, vilket passar ett utbildningsprojekt och lokal utveckling. Nackdelen är att cachen är processbunden och inte fungerar om appen körs i flera instanser eller om processen startas om. Redis skulle ge delad cache mellan instanser och bättre skalbarhet, men kräver drift, konfiguration och mer kod för connection-hantering. Vi valde därför IMemoryCache för snabbhet och enkelhet, men dokumenterade att en framtida skalning bör använda distribuerad cache. Ett alternativ hade också varit att helt förlita sig på databasen och inte cacha alls, men det skulle öka API-anrop och göra appen långsammare.

---

### 11. Vidareutveckling


Först skulle jag prioritera testning: enhets- och integrationstester för `WeatherService` och `WeatherApiClient` med mockade beroenden, samt en in-memory-databas för repository. Därefter skulle jag stärka säkerhet och konfiguration genom att flytta API-nyckelhantering till säkra secrets-stores och lägga till tydliga felmeddelanden i UI. Jag skulle också införa en tydligare domänmodell (t.ex. separat entitet för stad och historik) för bättre normalisering och framtida funktioner som favoritstäder. Slutligen vill jag skala prestanda med distribuerad cache (Redis), rate-limiting och bakgrundsjobb för att uppdatera populära städer automatiskt, samt eventuellt lägga till ett REST-API för externa klienter.

---

## Kodbilaga (VG)


### Exempel 1: Builder-pattern för URL-byggnad

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
        => $"{_baseUrl}?q={Uri.EscapeDataString(_city)}&units={_units}&appid={apiKey}";
}
```

### Exempel 2: Cache hit/miss i WeatherApiClient

```csharp
public async Task<WeatherData?> FetchWeatherAsync(string city)
{
    var cacheKey = $"weatherdata_{city}";
    if (_cache.TryGetValue(cacheKey, out WeatherData? cached))
    {
        cached.Id = 0;
        return cached;
    }

    var json = await _client.GetStringAsync(_builder.WithCity(city).Build(_apiKey!));
    var newData = JsonSerializer.Deserialize<JsonElement>(json);
    var weather = new WeatherData { City = city, Temperature = newData.GetProperty("main").GetProperty("temp").GetDouble() };
    _cache.Set(cacheKey, weather, TimeSpan.FromMinutes(15));
    return weather;
}
```

### Exempel 3: DI-registrering i Program.cs

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=weather.db"));

builder.Services.AddHttpClient<IWeatherApiClient, WeatherApiClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddScoped<WeatherService>();
builder.Services.AddMemoryCache();
```

### 12. Arbetsprocess och verktyg


Jag använde kursmaterialet om caching och design patterns, samt projektets egen kod och dokumentation för att säkerställa att beskrivningen matchar vår implementation. Jag diskuterade vissa punkter med gruppmedlemmar för att stämma av roller och flöden. Jag använde även ett AI-verktyg för att strukturera texten och kontrollera att varje fråga besvarades med tillräcklig teknisk detalj. Allt som skrevs verifierades mot faktiska filer i projektet (Program.cs, WeatherApiClient, WeatherService, HomeController) för att undvika att jag skrev om funktioner vi inte har.

---
