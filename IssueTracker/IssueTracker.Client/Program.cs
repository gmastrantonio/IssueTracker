using IssueTracker.Client;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using IssueTracker.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("https://localhost:7245/") // <-- Sostituisci con la porta reale delle tue API (es. 7100)
});

// 1. Registra l'handler tra i servizi
builder.Services.AddTransient<JwtAuthorizationHandler>();

// 2. Configura HttpClient per utilizzare il JwtAuthorizationHandler tramite l'HTTP Client Factory
builder.Services.AddHttpClient("IssueTracker.API", client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
})
.AddHttpMessageHandler<JwtAuthorizationHandler>();

// 3. Registra l'HttpClient predefinito iniettando quello configurato sopra
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("IssueTracker.API"));

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
// Registra il tuo AuthStateProvider anche come classe concreta per iniettarlo nei componenti di Login
builder.Services.AddScoped<CustomAuthenticationStateProvider>(provider =>
    (CustomAuthenticationStateProvider)provider.GetRequiredService<AuthenticationStateProvider>());

builder.Services.AddScoped<IAuthService, AuthService>();


await builder.Build().RunAsync();
