using Microsoft.EntityFrameworkCore;
using WeatherInsight.ApiClients;
using WeatherInsight.Data;
using WeatherInsight.Repositories;
using WeatherInsight.Services;

// Step 1: Bootstrap the generic ASP.NET Core builder that collects config + DI setup.
var builder = WebApplication.CreateBuilder(args);

// Step 2: Register EF Core with a SQLite provider so DbContext can be injected anywhere.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=weather.db"));

// Step 3: Register the repository abstraction for callers that want persistence-only access.
builder.Services.AddScoped<IWeatherRepository, WeatherRepository>();

// Step 4: Register the request builder as a singleton since it is a stateless helper.
builder.Services.AddSingleton<WeatherRequestBuilder>();

// Step 5: Register the typed HttpClient so WeatherApiClient gets an HttpClient instance plus DI wiring.
builder.Services.AddHttpClient<IWeatherApiClient, WeatherApiClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Step 6: Register the WeatherService so controllers can orchestrate API + database operations.
builder.Services.AddScoped<WeatherService>();

// Step 7: Add MVC controller + Razor view support required by the app UI.
builder.Services.AddControllersWithViews();

// Step 8: Materialize the WebApplication with all the above configuration.
var app = builder.Build();

// Step 9: Automatically apply EF Core migrations to ensure DB exists and is up-to-date.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Step 10: Configure production-safe error handling and HSTS when we are outside Development.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Step 11: Enforce HTTPS and serve static files (css/js) before hitting MVC.
app.UseHttpsRedirection();
app.UseStaticFiles();

// Step 12: Enable routing + authorization middleware in the standard order.
app.UseRouting();

// Add a few conservative security headers.
app.Use(async (context, next) =>
{
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
    context.Response.Headers.TryAdd("Referrer-Policy", "no-referrer");
    await next();
});

app.UseAuthorization();

// Step 13: Map the default controller route (Home/Index) for incoming requests.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Step 14: Run the configured web host.
app.Run();
