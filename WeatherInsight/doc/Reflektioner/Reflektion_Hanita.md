# Reflektion över Grupprojekt – Advanced OOP

**Namn:** Hanita Melin  
**Grupp:** WeatherInsight  
**Datum:** 26/11/2025  
**Antal personer i gruppen:** 3

---

## **Del 1: Grundreflektion (G)**

### 1. Projektöversikt och din roll

Projektet _WeatherInsight_ syftade till att skapa en applikation som hämtar, lagrar och presenterar väderdata från ett externt API. Applikationen använder **ASP.NET Core**, **Entity Framework Core**, **SQLite**, och följer **MVC-arkitektur** med separerade lager för presentation, affärslogik och datalagring.

Min primära roll var **backendutvecklare** i projektets första fas. Jag ansvarade för att skapa **modell- och datalagret**, implementera **EF Core**, definiera **DbContext**, bygga grundstrukturen för **Repository- och Service-lager**, samt utveckla **WeatherApiClient** för API-anrop. Jag arbetade inte med frontend, caching, eller initial API-nyckelhantering, men senare har jag försökt integrerade **User Secrets** för säkra API-nycklar, pågående projekt. 

---

### 2. Teknisk implementation

Dataflödet i applikationen fungerar så här: `WeatherApiClient` skickar HTTP-förfrågningar till ett väder-API och deserialiserar JSON-svar till `WeatherData`-objekt. Dessa objekt kan sedan sparas via **WeatherRepository** i EF Core-databasen.  
Service-lagret, **WeatherService**, ansvarar för affärslogik som historikbegränsning (sista 10 posterna) och sortering inför presentation.

När data hämtas från API sker det via `HttpClient` och DI hanterar livscykeln. Databasen uppdateras med `DbSet<WeatherData>` och automatisk migration sker vid applikationsstart, vilket säkerställer att systemet fungerar på **Windows, Mac och Linux**.

Caching implementerades inte av mig, utan var planerat för senare. Detta innebär att alla anrop från API går direkt till servern och sparas i EF Core innan de används av presentationen.

---

### 3. Design Patterns

**Pattern 1: Builder**

- **Problem:** Flexibel konfiguration av API-anrop (koordinater, endpoint, URL).
    
- **Lösning:** `WeatherApiClient` använder Builder-pattern för stegvis konfigurering.
    
- **Kodexempel:**
    

`var client = new WeatherApiClient(httpClient)                 .WithCoordinates(59.3, 18.1)                 .WithCustomUrl("https://api.open-meteo.com/v1/forecast"); var weather = await client.GetWeatherAsync();`

**Pattern 2: Repository**

- **Problem:** Separera databaslogik från affärslogik och göra koden testbar.
    
- **Lösning:** `WeatherRepository` kapslar in EF Core-åtkomst och hanterar CRUD-operationer.
    
- **Kodexempel:**
    

`public class WeatherRepository {     private readonly AppDbContext _context;     public WeatherRepository(AppDbContext context) { _context = context; }     public async Task AddWeatherAsync(WeatherData data) {         _context.WeatherDatas.Add(data);         await _context.SaveChangesAsync();     } }`

**Pattern 3: MVC (Model-lagret)**

- **Problem:** Separera presentation, affärslogik och datalagring.
    
- **Lösning:** Jag byggde upp Models och DbContext som Controllers och Views sedan använder.
    
- **Kodexempel:**
    

`public class WeatherData {     public int Id { get; set; }     public string City { get; set; }     public double Temperature { get; set; }     public DateTime Date { get; set; } }`

---

### 4. Utmaningar

- **Plattformskompatibla migrationer:** SQLite fungerade olika på Windows/Mac/Linux → löstes med automatisk migrering i `Program.cs`.
    
- **Flexibel API-struktur:** Byta API skulle bli krångligt → löstes med Builder-pattern och ren service-struktur.
    
- **Samtidiga anrop:** Risk för socket exhaustion → DI hanterar `HttpClient`.
    
- **Historikbegränsning:** Växande databas → hanteras i Service-lagret med begränsning till 10 poster.
    
- **API-nyckelhantering:** Miljövariabler fungerade dåligt → implementerade User Secrets.
    

Nästa gång skulle jag tidigt definiera **plattformstest och CI/CD** för att undvika migrationsproblem.

---

### 5. Lärdomar

- Arkitektur blir stark med tydlig separation av ansvar.
    
- Builder-, Repository- och MVC-mönster gör koden testbar och modulär.
    
- EF Core + SQLite ger enkel, skalbar lösning över plattformar.
    
- Separering mellan API-klient, service och repository underlättar vidareutveckling.
    
- Viktigt att planera för plattformsoberoende från start.
    
- Samarbete i gruppen kräver tydlig ansvarsfördelning.
    

---

### 6. Grupparbetet

- **Medlemmar:** [Claes och Stephen]
    
- **Samarbete:** Gruppen fungerade harmoniskt med hög kreativitet. Konflikter var få, främst kring API-struktur och designval, och löstes genom diskussion.
    

---

## **Del 2: Fördjupning (VG)**

### 7. Arkitektur och kodkvalitet

Koden är strukturerad i lager: **Presentation (Controllers/Views)**, **Business (WeatherService)**, **Data (Repository/DbContext/Models)**. DI används för HttpClient, repository och service, vilket gör komponenter testbara isolerat. Exempelvis kan `WeatherRepository` testas med en in-memory DB utan att påverka Controllers. Tight coupling undviks genom interface-baserad design och tydlig separation mellan lager.

---

### 8. Cache-strategi och prestanda

Caching implementerades inte, men systemet är designat för att enkelt kunna införa caching. Vid implementation skulle man ha: TTL, cache hit/miss-hantering och async/await för API-anrop. Om jag byggde om lösningen skulle jag lägga till ett **in-memory cache-lager i service-lagret**, med TTL på 5–10 minuter och automatisk invalidation vid ny data.

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

1. **Caching:** Implementera TTL-cache för att minska API-anrop.
    
2. **Fullständig User Secrets-integration:** Säker API-nyckelhantering för alla miljöer.
    
3. **Unit testing:** Testa service- och repository-lager med mockade API-anrop och in-memory DB.
    
4. **Strategi för flera API-källor:** Fortsätta med Strategy-pattern för fler vädertjänster.
    
5. **Frontendförbättringar:** Koppla MVC-lagren till responsiv UI med live-data.
    

---

### 12. Arbetsprocess och verktyg

- **Verktyg:** Visual Studio, GitHub, EF Core dokumentation, kursmaterial, diskussioner med kollegor.
    
- **AI-användning:** ChatGPT användes för strukturering av reflektionen, mallhantering och kodexempel. All information verifierades mot den faktiska koden och projektets implementationsval.
    

---

### **Kodbilaga**

**Exempel 1:** Builder-pattern i API-klienten

`var client = new WeatherApiClient(httpClient)                 .WithCoordinates(59.3, 18.1)                 .WithCustomUrl("https://api.open-meteo.com/v1/forecast"); var weather = await client.GetWeatherAsync();`

**Exempel 2:** Repository-mönster

`public async Task AddWeatherAsync(WeatherData data) {     _context.WeatherDatas.Add(data);     await _context.SaveChangesAsync(); }`

**Exempel 3:** Model för EF Core / MVC

`public class WeatherData {     public int Id { get; set; }     public string City { get; set; }     public double Temperature { get; set; }     public DateTime Date { get; set; } }`