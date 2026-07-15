using IssueTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddSwaggerGen();

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
app.UseAuthorization();

// Mappa gli endpoint dei Controller
app.MapControllers();

app.Run();