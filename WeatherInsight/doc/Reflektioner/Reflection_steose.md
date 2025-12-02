# Reflektion över Grupprojekt – Advanced OOP Group2

**Namn:** Osedumme Stephen Uche  
**Grupp:** WeatherInsight  
**Datum:** 30/11/2025  
**Antal personer i gruppen:** 3

## Vad Levererades

* Omgjorde dependency injection så att `WeatherService`, `WeatherApiClient` och
  `WeatherRequestBuilder` nu löses automatiskt och finns tillgängliga för `HomeController`.

```csharp
// Program.cs
builder.Services.AddScoped<IWeatherRepository, WeatherRepository>();
builder.Services.AddSingleton<WeatherRequestBuilder>();
builder.Services.AddHttpClient<IWeatherApiClient, WeatherApiClient>(client =>
{
client.Timeout = TimeSpan.FromSeconds(10);
})
.AddTransientHttpErrorPolicy(policy =>
policy.WaitAndRetryAsync(3, retry => TimeSpan.FromMilliseconds(300 * retry)))
.AddTransientHttpErrorPolicy(policy =>
policy.CircuitBreakerAsync(
handledEventsAllowedBeforeBreaking: 3,
durationOfBreak: TimeSpan.FromSeconds(10)));
builder.Services.AddScoped<WeatherService>();
// HomeController.cs
public HomeController(ILogger<HomeController> logger, WeatherService weatherService,
IMemoryCache cache)
{
_logger = logger;
_cache = cache;
_weatherService = weatherService;
}
```

* Implementerade robust felhantering/loggning i `WeatherApiClient` för att förhindra krascher
  när OpenWeather avvisar en förfrågan.

```csharp
// WeatherApiClient.cs
try
{
json = await _client.GetStringAsync(url);
catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
}
{
throw new ArgumentException("City name is wrong");
}
// HomeController.cs
catch (ArgumentException ex) when (ex.Message == "City name is wrong")
{
_logger.LogWarning(ex, "Invalid city input: {City}", city);
ViewData["Error"] = ex.Message;
}
```

* Återbyggde Home-vyn för att visa den senaste avläsningen, historiktabell samt ett formulär
  som reflekterar aktuell stadsinmatning.

```csharp
<!-- Views/Home/Index.cshtml -->
<form method="get" asp-controller="Home" asp-action="Index" class="row g-3 mb-4">
<input type="text" id="city" name="city" class="form-control" value="@selectedCity"
placeholder="Enter city name" />
</form>
<div class="card mb-4">
@if (latest is not null)
{
<h2 class="h4 mb-3">Latest Reading for @latest.City</h2>
<p class="mb-1"><strong>Temperature:</strong> @latest.Temperature&deg; C</p>
<p class="mb-1"><strong>Condition:</strong> @latest.Condition
@GetConditionEmoji(latest.Condition)</p>
}
</div>
```

* Dokumentationen du läser nu sammanställdes, samt utökad README-vägledning.

## Utmaningar & Lösningar

1. **401 Unauthorized Responses:** API-klienten kastade ursprungligen undantag när
   OpenWeather-nyckeln saknades/var ogiltig. Vi lade till explicita kontroller, loggning och
   konfigurationsråd så att UI:t kan återhämta sig smidigt.

```csharp
// WeatherApiClient.cs
_apiKey = Environment.GetEnvironmentVariable("OPENWEATHER_API_KEY");
if (string.IsNullOrEmpty(_apiKey))
{
throw new InvalidOperationException("Missing OpenWeather API key. Configure
'OPENWEATHER_API_KEY' via environment variables.");
}
```

2. **Servicekoppling:** Ursprungskoden refererade till ett icke-existerande `WeatherForge`-namespace.
   Uppdatering av namespaces, DI-registreringar och controller-injektioner säkerställde att tjänsten
   faktiskt används.

```csharp
// Program.cs
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<WeatherService>();
// HomeController.cs
WeatherData? latest = await _weatherService.FetchAndSaveAsync(city);
var history = await _weatherService.GetAllAsync();
```

3. **Användarupplevelse:** Utan ett UI som visade sparad data var det svårt att verifiera funktionalitet.
   Den nya Index-vyn och ViewData-användningen ger omedelbar feedback, vilket gör felsökning enklare.

```csharp
// HomeController.cs
ViewData["SelectedCity"] = city;
ViewData["LatestWeather"] = latest;
ViewData["History"] = history;
<!-- Views/Home/Index.cshtml -->
@if (history.Any())
{
<table class="table table-striped align-middle">
<thead>
<tr>
<th>City</th>
<th>Temperature (&deg;C)</th>
<th>Condition</th>
<th>Fetched At</th>
</tr>
</thead>
<tbody>
@foreach (var entry in history)
{
<tr>
<td>@entry.City</td>
<td>@entry.Temperature</td>
<td>@entry.Condition @GetConditionEmoji(entry.Condition)</td>
<td>@entry.Date.ToString("yyyy-MM-dd HH:mm")</td>
</tr>
}
</tbody>
</table>
}
```

## Lärdomar

* Typed `HttpClient`-registreringar förenklar delad konfiguration och gör det möjligt att lägga till
  loggning/kontroller på ett ställe.
* Att hålla hemligheter (API-nycklar) konfigurationsstyrda via appsettings + user-secrets
  förhindrar oavsiktliga commits av privata värden.
* Steg-för-steg-dokumentation och kommentarer hjälper framtida utvecklare att följa flöden utan
  att behöva leta i varje fil.

## Nästa Steg

1. Lägg till enhets-/integrationstester kring `WeatherService` med en mockad `IWeatherApiClient`.
2. Visa tydliga felmeddelanden i UI:t när API:t returnerar null så användaren vet att kontrollera sin nyckel.
3. Utöka domänmodellen för att inkludera luftfuktighet/vind eller flera leverantörer, vilket demonstrerar
   nyttan av service-/repository-abstraktionen.

## Begränsningar & Framtida Förbättringar

* Ingen klientvalidering eller auto-komplettering för stadsnamn.
* Weather-historik är global; filtrering per användare/autentisering är inte implementerad.
* Tester saknas; överväg att lägga till tjänstenivå-tester med mockad `IWeatherApiClient`.
* Förbättra UX genom att visa tydligare felmeddelanden när hämtningar misslyckas eller när API-kvoten överskrids.

---


