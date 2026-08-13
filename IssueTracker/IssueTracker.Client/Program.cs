using IssueTracker.Client;
using IssueTracker.Client.Interfaces.Services;
using IssueTracker.Client.Services;
using IssueTracker.Client.Services.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

// Inizializza il builder per l'hosting dell'applicazione Blazor WebAssembly
var builder = WebAssemblyHostBuilder.CreateDefault(args);

// =========================================================================
// 1. CONFIGURAZIONE DEI COMPONENTI ROOT DI BLAZOR
// =========================================================================

// Mappa il componente principale <App /> sull'elemento HTML con id="app" (solitamente in index.html)
builder.RootComponents.Add<App>("#app");

// Consente a Blazor di gestire dinamicamente il tag <head> (titoli pagina, meta tag, ecc.)
builder.RootComponents.Add<HeadOutlet>("head::after");

// =========================================================================
// 2. REGISTRAZIONE DEI SERVIZI (Iniezione delle Dipendenze - IoC Container)
// =========================================================================

// 2.1 Gestione del Token JWT nell'HTTP Pipeline
// Registra il DelegatingHandler come Transient (viene creata una nuova istanza per ogni messaggio HTTP/client).
// Questo handler intercetta le chiamate e aggiunge l'header 'Authorization: Bearer <token>' se disponibile.
builder.Services.AddTransient<JwtAuthorizationHandler>();

// 2.2 Configurazione di HttpClient tramite IHttpClientFactory
// Registra un client HTTP con nome ("IssueTracker.API"), impostando l'URL base del backend
// e collegando l'handler 'JwtAuthorizationHandler' alla sua pipeline di esecuzione.
builder.Services.AddHttpClient("IssueTracker.API", client =>
{
    // Nota: in produzione è consigliabile leggere questo valore da appsettings.json
    client.BaseAddress = new Uri("https://localhost:7245/");
})
.AddHttpMessageHandler<JwtAuthorizationHandler>();

// 2.3 Registrazione del HttpClient di default
// Sostituisce la registrazione predefinita di HttpClient iniettando direttamente 
// il client configurato al punto 2.2 ("IssueTracker.API") completo di JwtAuthorizationHandler.
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("IssueTracker.API"));

// 2.4 Servizi di Autenticazione e Stato Utente
// Attiva i servizi fondamentali di autorizzazione lato client per Blazor WebAssembly
builder.Services.AddAuthorizationCore();

// Registra la gestione dello stato di autenticazione personalizzato (CustomAuthenticationStateProvider).
// Sostituisce l'AuthenticationStateProvider di base di Blazor per notificare il cambio di stato dell'utente.
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// Registra la classe concreta CustomAuthenticationStateProvider riutilizzando l'istanza già creata sopra.
// Utile se si desidera iniettare direttamente CustomAuthenticationStateProvider nei componenti di Login/Logout.
builder.Services.AddScoped<CustomAuthenticationStateProvider>(provider =>
    (CustomAuthenticationStateProvider)provider.GetRequiredService<AuthenticationStateProvider>());

// 2.5 Servizi applicativi / Business Logic
// Registra i servizi di comunicazione con l'API per l'Autenticazione e per i Ticket/Issue.
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITicketService, TicketService>();

// =========================================================================
// 3. AVVIO DELL'APPLICAZIONE BLAZOR
// =========================================================================

// Costruisce ed esegue in modo asincrono l'applicazione WebAssembly all'interno del browser
await builder.Build().RunAsync();