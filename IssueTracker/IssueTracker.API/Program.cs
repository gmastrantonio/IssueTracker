using IssueTracker.Core.Interfaces;
using IssueTracker.Infrastructure.Data;
using IssueTracker.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

//registrazione servizio per autenticazione token
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]!)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// Registra il password hasher
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

// ==========================================
// 1. REGISTRAZIONE DEI SERVIZI (Prima di Build)
// ==========================================

// Recupera la stringa di connessione dal file appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Registra il DataContext configurato con SQL Server
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(connectionString, b => b.MigrationsAssembly("IssueTracker.Infrastructure")));

// Registra i Controller (basta una volta sola!)
builder.Services.AddControllers();

// Registra i servizi per Swagger
builder.Services.AddEndpointsApiExplorer();
// ...

builder.Services.AddSwaggerGen(options =>
{
    // 1. Definiamo lo schema di sicurezza Bearer
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Inserisci 'Bearer' seguito da uno spazio e dal tuo token JWT."
    });

    // 2. Usiamo la lambda doc => ... per passare il requisito
    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });
});
// Configura la policy CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorClientPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ==========================================
// 2. COSTRUZIONE DELL'APPLICAZIONE
// ==========================================
var app = builder.Build(); // <--- Da questo punto in poi i servizi sono bloccati!

// ==========================================
// 3. CONFIGURAZIONE DEI MIDDLEWARE (Dopo Build)
// ==========================================

// Abilita Swagger in ambiente di sviluppo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Abilita il middleware CORS (deve essere prima di MapControllers!)
app.UseCors("BlazorClientPolicy");

app.UseHttpsRedirection();
app.UseAuthentication(); // DEVE stare PRIMA di app.UseAuthorization()
app.UseAuthorization();

// Mappa gli endpoint dei Controller
app.MapControllers();

app.Run();