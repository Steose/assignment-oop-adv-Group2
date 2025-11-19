# WeatherInsight Documentation

## Overview
- **Purpose:** WeatherInsight is an ASP.NET Core MVC application that retrieves current conditions from the OpenWeather API and persists each lookup to SQLite for later review.
- **Key Capabilities:** Search by city, capture responses in a local database, and present the latest reading alongside the full history.
- **Tech Stack:** .NET 9, ASP.NET Core MVC, EF Core with SQLite, typed `HttpClient` integration.

## High-Level Architecture
1. **Presentation Layer:** `HomeController` plus Razor views render the UI and expose a simple form for city queries.
2. **Application Layer:** `WeatherService` orchestrates API calls and database writes/reads.
3. **Integration Layer:** `WeatherApiClient` and `WeatherRequestBuilder` construct and issue HTTP requests to OpenWeather.
4. **Persistence Layer:** `AppDbContext`, `WeatherRepository`, and the `WeatherData` entity map to a SQLite database (`weather.db`).
5. **Configuration:** `Program.cs` wires up dependency injection for every component and reads secrets (OpenWeather API key) from configuration.

```
Browser -> HomeController -> WeatherService -> (WeatherApiClient -> OpenWeather)
                                   |
                                   v
                             AppDbContext (SQLite)
```

## Component Reference
| Layer | Key Types | Responsibilities |
| --- | --- | --- |
| Presentation | `HomeController`, `Views/Home/Index.cshtml` | Accept city input, show latest result + history via `ViewData` |
| Application | `WeatherService` | Coordinate fetching data and persisting it, expose reusable async methods |
| Integration | `WeatherApiClient`, `WeatherRequestBuilder`, `IWeatherApiClient` | Build URLs, call OpenWeather, deserialize JSON, log failures |
| Persistence | `AppDbContext`, `WeatherData`, `IWeatherRepository` | Provide EF Core DbSets/repositories for SQLite storage |
| Configuration | `Program.cs`, `appsettings*.json` | Register services (DbContext, HttpClient, WeatherService, RequestBuilder) and manage settings |

## Configuration & Secrets
- `appsettings.json` / `.Development.json` include the `OpenWeather:ApiKey` slot. For production, set this value via *User Secrets* or environment variables:
  ```bash
  dotnet user-secrets set OpenWeather:ApiKey YOUR_KEY --project WeatherInsight
  ```
- Ensure the SQLite file path in `Program.cs` (`Data Source=weather.db`) points to a writable location when hosting.

## Data Persistence
- `WeatherData` holds `City`, `Temperature`, `Condition`, and `Date`.
- `AppDbContext` exposes `DbSet<WeatherData>` backed by SQLite.
- The included `weather.db` contains seed/history data. To recreate from scratch:
  ```bash
  rm WeatherInsight/weather.db
  dotnet ef database update
  ```

## Request Flow
1. User submits the Index page form (`GET /?city=...`).
2. `HomeController.Index` injects `WeatherService`, invokes `FetchAndSaveAsync(city)` and `GetAllAsync()`.
3. `WeatherService.FetchAndSaveAsync`:
   - Guards against null API responses.
   - Calls `WeatherApiClient.FetchWeatherAsync`.
   - Persists the entity via `AppDbContext`.
4. `WeatherApiClient` builds the OpenWeather URL, validates the API key, issues the HTTP GET, parses JSON, and logs errors (401s, malformed payloads).
5. View renders `ViewData["LatestWeather"]` and `ViewData["History"]`.

## Running Locally
```bash
cd WeatherInsight
dotnet OPENWEATHER_API_KEY="Your-API-Key " restore
dotnet build
dotnet run
```
- Visit `https://localhost:5001` (or the console output URL).
- Ensure the OpenWeather API key is configured before running; otherwise the UI shows “No weather data available”.

