using IssueTracker.Core.DTOs;
using IssueTracker.Core.Models;
using IssueTracker.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IssueTracker.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Tutti gli endpoint richiedono un Token JWT valido
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

            // Recupera l'ID o lo Username dell'utente dal Token JWT
            var username = User.Identity?.Name;

            // Oppure recupera un Claim specifico (es. NameIdentifier)
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;


            // Mappiamo il CreateTicketDto sull'entità di dominio reale per il database
            var newTicket = new Ticket
            {
                Title = dto.Title,
                Description = dto.Description,
                Priority = dto.Priority,
                Status = TicketStatus.New, // Un nuovo ticket parte sempre come "New"
                CreatedAt = DateTime.UtcNow,
                AuthorId = int.Parse(userId)
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

        // PUT: api/tickets/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTicket(int id, [FromBody] UpdateTicketDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Cerchiamo il ticket nel database
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound($"Ticket con ID {id} non trovato.");
            }

            // Aggiorniamo le proprietà con i dati provenienti dal DTO
            ticket.Title = dto.Title;
            ticket.Description = dto.Description;
            ticket.Priority = dto.Priority;
            ticket.Status = dto.Status;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TicketExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent(); // Risposta standard 204 per i PUT andati a buon fine senza corpo di ritorno
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // Solo gli utenti con Role == "Admin" nel Token
        public async Task<IActionResult> DeleteTicket(int id)
        {
            // Cerchiamo il ticket nel database
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound($"Ticket con ID {id} non trovato.");
            }

            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();
            return Ok();
        }

        private bool TicketExists(int id)
        {
            return _context.Tickets.Any(e => e.Id == id);
        }
    }
}