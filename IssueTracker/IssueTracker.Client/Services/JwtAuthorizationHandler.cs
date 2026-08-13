using System.Net;
using System.Net.Http.Headers;
using IssueTracker.Client.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace IssueTracker.Client.Services;

public class JwtAuthorizationHandler : DelegatingHandler
{
    private readonly IJSRuntime _js;
    private readonly IServiceProvider _serviceProvider;

    public JwtAuthorizationHandler(IJSRuntime js, IServiceProvider serviceProvider)
    {
        _js = js;
        _serviceProvider = serviceProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // 1. Aggiunge il token corrente se disponibile
        var token = await GetTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        // 2. Invia la richiesta HTTP originale al backend
        var response = await base.SendAsync(request, cancellationToken);

        // 3. Se la risposta è 401 Unauthorized, tentiamo il Refresh silente
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Risolviamo IAuthService dallo Scope del ServiceProvider per evitare riferimenti circolari
            using var scope = _serviceProvider.CreateScope();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

            // Tenta di rinnovare il token
            var newToken = await authService.RefreshTokenAsync();

            if (!string.IsNullOrWhiteSpace(newToken))
            {
                // Clona la richiesta originale e aggiorna l'header Authorization col nuovo token
                var newRequest = await CloneHttpRequestMessageAsync(request);
                newRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);

                // Riprova la richiesta verso le API
                response.Dispose(); // libera la risposta 401 precedente
                response = await base.SendAsync(newRequest, cancellationToken);
            }
        }

        return response;
    }

    private async Task<string?> GetTokenAsync()
    {
        try
        {
            return await _js.InvokeAsync<string>("localStorage.getItem", "authToken");
        }
        catch
        {
            return null;
        }
    }

    // Helper per clonare la richiesta HTTP originale prima di reinviarla
    private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage req)
    {
        var clone = new HttpRequestMessage(req.Method, req.RequestUri);

        if (req.Content != null)
        {
            var ms = new MemoryStream();
            await req.Content.CopyToAsync(ms);
            ms.Position = 0;
            clone.Content = new StreamContent(ms);

            if (req.Content.Headers.ContentType != null)
            {
                clone.Content.Headers.ContentType = req.Content.Headers.ContentType;
            }
        }

        foreach (var header in req.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }
}