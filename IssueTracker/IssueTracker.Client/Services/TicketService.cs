using IssueTracker.Client.Interfaces.Services;
using IssueTracker.Core.DTOs; // Adatta il namespace per i tuoi modelli/DTO
using System.Net.Http.Json;

namespace IssueTracker.Client.Services;

public class TicketService : ITicketService
{
    private readonly HttpClient _httpClient;

    public TicketService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // 1. LETTURA (Get All)
    public async Task<List<TicketResponseDto>> GetTicketsAsync()
    {
        try
        {
            var tickets = await _httpClient.GetFromJsonAsync<List<TicketResponseDto>>("api/tickets");
            return tickets ?? new List<TicketResponseDto>();
        }
        catch
        {
            return new List<TicketResponseDto>();
        }
    }

    // 2. LETTURA SINGOLA (Get by Id)
    public async Task<TicketResponseDto?> GetTicketByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<TicketResponseDto>($"api/tickets/{id}");
        }
        catch
        {
            return null;
        }
    }

    // 3. CREAZIONE (Post)
    public async Task<CreateTicketDto?> CreateTicketAsync(CreateTicketDto createDto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/tickets", createDto);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<CreateTicketDto>();
        }

        return null;
    }

    // 4. MODIFICA (Put)
    public async Task<bool> UpdateTicketAsync(int id, UpdateTicketDto updateDto)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/tickets/{id}", updateDto);
        return response.IsSuccessStatusCode;
    }

    // 5. ELIMINAZIONE (Delete)
    public async Task<bool> DeleteTicketAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"api/tickets/{id}");
        return response.IsSuccessStatusCode;
    }
}