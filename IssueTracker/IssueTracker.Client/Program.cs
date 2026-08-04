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

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
// Registra il tuo AuthStateProvider anche come classe concreta per iniettarlo nei componenti di Login
builder.Services.AddScoped<CustomAuthenticationStateProvider>(provider =>
    (CustomAuthenticationStateProvider)provider.GetRequiredService<AuthenticationStateProvider>());

builder.Services.AddScoped<IAuthService, AuthService>();


await builder.Build().RunAsync();
