using IssueTracker.Core.DTOs;
using IssueTracker.Core.Models;
using IssueTracker.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TicketsController : ControllerBase
    {
        private readonly DataContext _context;

        public TicketsController(DataContext context)
        {
            _context = context;
        }

        // GET: api/tickets
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TicketResponseDto>>> GetTickets()
        {
            // Recuperiamo i ticket dal database e li mappiamo sui nostri Response DTO
            var tickets = await _context.Tickets
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TicketResponseDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    Status = t.Status.ToString(),
                    Priority = t.Priority.ToString(),
                    CreatedAt = t.CreatedAt,
                    AuthorName = "Utente di Test" // Sostituiremo con il nome reale quando avremo gli utenti
                })
                .ToListAsync();

            return Ok(tickets);
        }

        // POST: api/tickets
        [HttpPost]
        public async Task<ActionResult<TicketResponseDto>> CreateTicket([FromBody] CreateTicketDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Mappiamo il CreateTicketDto sull'entità di dominio reale per il database
            var newTicket = new Ticket
            {
                Title = dto.Title,
                Description = dto.Description,
                Priority = dto.Priority,
                Status = TicketStatus.New, // Un nuovo ticket parte sempre come "New"
                CreatedAt = DateTime.UtcNow,
                AuthorId = dto.AuthorId
            };

            _context.Tickets.Add(newTicket);
            await _context.SaveChangesAsync();

            // Mappiamo l'entità salvata sul Response DTO per ritornarlo al client
            var responseDto = new TicketResponseDto
            {
                Id = newTicket.Id,
                Title = newTicket.Title,
                Description = newTicket.Description,
                Status = newTicket.Status.ToString(),
                Priority = newTicket.Priority.ToString(),
                CreatedAt = newTicket.CreatedAt,
                AuthorName = "Utente di Test"
            };

            return CreatedAtAction(nameof(GetTickets), new { id = newTicket.Id }, responseDto);
        }
    }
}