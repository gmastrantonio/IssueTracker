using IssueTracker.Core.DTOs; // Adatta il namespace in base a dove risiedono i DTO/Modelli dei Ticket

namespace IssueTracker.Client.Interfaces.Services;

public interface ITicketService
{
    Task<List<TicketResponseDto>> GetTicketsAsync();
    Task<TicketResponseDto?> GetTicketByIdAsync(int id);
    Task<CreateTicketDto?> CreateTicketAsync(CreateTicketDto createDto);
    Task<bool> UpdateTicketAsync(int id, UpdateTicketDto updateDto);
    Task<bool> DeleteTicketAsync(int id);
}