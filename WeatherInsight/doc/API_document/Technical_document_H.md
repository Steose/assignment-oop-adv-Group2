# **WeatherInsight – Technical Documentation**

## Overview

**Purpose:** WeatherInsight är en ASP.NET Core MVC-applikation som hämtar väderdata från externa API:er (Open-Meteo/OpenWeather) och sparar varje uppslag i SQLite för senare granskning.

**Key Capabilities:**

- Hämta aktuell väderdata via stad.
    
- Spara varje uppslag i lokal SQLite-databas.
    
- Visa senaste väderpost tillsammans med historik (max 10 poster).
    

**Tech Stack:**

- .NET 9, ASP.NET Core MVC
    
- EF Core med SQLite
    
- Typed HttpClient + Builder pattern
    
- Polly för retry och circuit-breaker
    

---

## High-Level Architecture

`Browser -> HomeController -> WeatherService -> (WeatherApiClient -> External API)                                    |                                    v                              AppDbContext (SQLite)`

### Layers

|Layer|Key Types|Responsibilities|
|---|---|---|
|**Presentation**|`HomeController`, Razor Views|Tar emot användarinmatning (stad), visar senaste väder + historik|
|**Application**|`WeatherService`|Orkestrerar API-anrop och lagrar data, exponerar återanvändbara asynkrona metoder|
|**Integration**|`WeatherApiClient`, `WeatherRequestBuilder`, `IWeatherApiClient`|Bygger URL:er, utför HTTP-anrop, deserialiserar JSON, hanterar fel (retry, circuit breaker via Polly)|
|**Persistence**|`AppDbContext`, `WeatherData`, `IWeatherRepository`|EF Core DbSets och repository för SQLite-lagring|
|**Configuration**|`Program.cs`|Registrerar DI, sätter upp DbContext, HttpClient, Service och Builder, hanterar framtida API-nycklar (via konsol/Play)|

---

## Configuration & Secrets

- För nuvarande hanteras API-nyckeln via konsolen (`dotnet run`) med användarinmatning.
    
- I framtiden kan detta automatiseras med Visual Studio Play eller User Secrets:
    

`dotnet user-secrets set OpenWeather:ApiKey YOUR_KEY --project WeatherInsight`

- SQLite-databasen (`weather.db`) ska ligga på skrivbar plats. För att återskapa databasen:
    

`rm WeatherInsight/weather.db dotnet ef database update`

---

## Data Persistence

- **`WeatherData`** innehåller `City`, `Temperature`, `Condition` och `Date`.
    
- **`AppDbContext`** exponerar `DbSet<WeatherData>` och används av repository för lagring och hämtning.
    
- Historiken begränsas till de senaste 10 posterna i service-lagret.
    

---

## Request Flow

1. Användaren skickar stad via formuläret på Index-sidan (GET /?city=...).
    
2. `HomeController.Index` anropar `WeatherService.FetchAndSaveAsync(city)` och `GetAllAsync()`.
    
3. `WeatherService.FetchAndSaveAsync`:
    
    - Kontrollerar null-response från API.
        
    - Anropar `WeatherApiClient.FetchWeatherAsync`.
        
    - Sparar posten via `WeatherRepository`/`AppDbContext`.
        
4. `WeatherApiClient` bygger URL med `WeatherRequestBuilder`, skickar GET, deserialiserar JSON och loggar fel.
    
5. View visar `ViewData["LatestWeather"]` och `ViewData["History"]`.
    

---

## Key Implementation Details

- **Builder Pattern:** Flexibel konfiguration av API-anrop (koordinater, URL, parametrar).
    
- **Polly:** Retry och circuit-breaker för robusta HTTP-anrop.
    
- **Dependency Injection:**
    
    - `WeatherService` och `WeatherRepository` som scoped.
        
    - `WeatherRequestBuilder` registrerad som singleton (klasskamrat).
        
- **EF Core Migration:** Automatiskt vid applikationsstart, fungerar plattformsoberoende (Windows, Mac, Linux).
    
- **Historikbegränsning:** Max 10 poster sparas och sorteras innan visning.
    
- **Samtidighet:** Genom återanvändning av `HttpClient` och korrekt DI hanteras flera användare samtidigt utan socket exhaustion.
    

---

## Running Locally

`cd WeatherInsight dotnet run`

- Besök `https://localhost:5160` (eller den URL som konsolen anger).
    
- Säkerställ att eventuella API-nycklar konfigureras innan körning, annars visas “No weather data available”.