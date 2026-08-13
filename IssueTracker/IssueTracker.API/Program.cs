using IssueTracker.Core.Interfaces;
using IssueTracker.Infrastructure.Data;
using IssueTracker.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// =========================================================================
// 1. REGISTRAZIONE DEI SERVIZI (Iniezione delle Dipendenze - IoC Container)
// =========================================================================

// Configura e registra la gestione dei Controller MVC/API.
// Permette al framework di mappare le classi marcate con [ApiController].
builder.Services.AddControllers();

// Registra i servizi di dominio e sicurezza con durata "Scoped" (una nuova istanza per ogni richiesta HTTP).
// - ITokenService: interfaccia per la generazione dei token JWT.
// - IPasswordHasher: interfaccia per l'hashing e la verifica delle password.
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

// Configura lo schema di autenticazione basato sul Bearer Token (JWT).
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Parametri di validazione applicati a ogni token JWT in ingresso nelle richieste HTTP
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Verifica che il token sia stato firmato con la nostra chiave segreta (evita manomissioni)
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]!)),

            // Verifica che il token sia stato emesso da un Issuer (emittente) fidato
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],

            // Verifica che il token sia destinato all'Audience (destinatario) corretto
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JwtSettings:Audience"],

            // Verifica che il token non sia scaduto (controlla le claim 'exp' e 'nbf')
            ValidateLifetime = true,

            // Imposta a zero la tolleranza temporale di default (normalmente 5 min) per far scadere il token all'ora esatta
            ClockSkew = TimeSpan.Zero
        };
    });

// Habilita i servizi per la gestione delle autorizzazioni (es. attributo [Authorize] sui Controller o metodi)
builder.Services.AddAuthorization();

// Recupera la stringa di connessione al database SQL Server dal file di configurazione appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Registra il DbContext di Entity Framework Core usando SQL Server.
// 'MigrationsAssembly' specifica la libreria (IssueTracker.Infrastructure) dove risiedono le migrazioni del DB.
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(connectionString, b => b.MigrationsAssembly("IssueTracker.Infrastructure")));

// Configurazione delle regole CORS (Cross-Origin Resource Sharing).
// Serve a consentire al browser di accettare chiamate HTTP provenienti da un dominio/porta differente (es. il client Blazor).
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        // Specifica le origini esatte del client frontend autorizzate a comunicare con l'API
        policy.WithOrigins("https://localhost:7007", "http://localhost:5007")
              .AllowAnyHeader()      // Consente qualsiasi header HTTP (compresi 'Authorization' e 'Content-Type')
              .AllowAnyMethod()      // Consente tutti i verbi HTTP (GET, POST, PUT, DELETE, OPTIONS)
              .AllowCredentials();   // Permette l'invio di credenziali/cookie se necessario
    });
});

// Configura i servizi per l'esplorazione degli endpoint API e la generazione automatica della documentazione Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Definisce lo schema di autenticazione "Bearer" nell'interfaccia grafica di Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Inserisci 'Bearer' seguito da uno spazio e dal tuo token JWT."
    });

    // Applica il requisito di sicurezza globale a Swagger per mostrare il lucchetto e inviare l'header di autenticazione
    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });
});

// =========================================================================
// 2. COSTRUZIONE E CONFIGURAZIONE DELLA PIPELINE DEI MIDDLEWARE
// =========================================================================

// Costruisce l'istanza dell'applicazione. Dopo questo punto non è più possibile registrare nuovi servizi.
var app = builder.Build();

// Se l'applicazione gira in ambiente di sviluppo, abilita l'interfaccia grafica di Swagger per testare gli endpoint
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Forza il reindirizzamento delle richieste HTTP verso il protocollo sicuro HTTPS
app.UseHttpsRedirection();

// --- NOTA SULL'ORDINE DEI MIDDLEWARE (Fondamentale per evitare errori 405/401) ---

// 1. Attiva il middleware di Routing per determinare quale controller/azione deve gestire la richiesta
app.UseRouting();

// 2. Middleware CORS: deve stare TASSATIVAMENTE tra UseRouting e UseAuthentication/UseAuthorization.
//    Gestisce le richieste preflight 'OPTIONS' inviate dai browser prima di una chiamata autenticata.
app.UseCors("AllowBlazorClient");

// 3. Middleware di Autenticazione: legge l'header Authorization, valida il token JWT e popola HttpContext.User
app.UseAuthentication();

// 4. Middleware di Autorizzazione: verifica se l'utente autenticato ha i permessi/ruoli necessari per la risorsa
app.UseAuthorization();

// Mappa le rotte individuate dai controller (es. [Route("api/[controller]")]) per eseguire le chiamate effettive
app.MapControllers();

// Avvia l'applicazione web e rimane in ascolto delle richieste HTTP in ingresso
app.Run();