using System.ComponentModel.DataAnnotations;
using IssueTracker.Core.Models;

namespace IssueTracker.Core.DTOs
{
    public class CreateTicketDto
    {
        [Required(ErrorMessage = "Il titolo è obbligatorio.")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "Il titolo deve essere lungo tra i 5 e i 100 caratteri.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descrizione è obbligatoria.")]
        [MinLength(10, ErrorMessage = "La descrizione deve contenere almeno 10 caratteri.")]
        public string Description { get; set; } = string.Empty;

        public TicketPriority Priority { get; set; } = TicketPriority.Medium;
        public int AuthorId { get; set; } = 1;
    }
}