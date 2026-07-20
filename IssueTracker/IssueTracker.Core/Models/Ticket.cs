using System;

namespace IssueTracker.Core.Models
{
    public class Ticket
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TicketStatus Status { get; set; } = TicketStatus.New;
        public TicketPriority Priority { get; set; } = TicketPriority.Medium;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Per ora usiamo un id autore fittizio, utile per quando implementeremo gli utenti
        // Chi ha aperto il ticket
        public int AuthorId { get; set; }
        public User Author { get; set; } = null!;

        // A chi è assegnato il ticket (nullable perché all'inizio può non essere assegnato)
        public int? AssignedToId { get; set; }
        public User? AssignedTo { get; set; }
    }
}