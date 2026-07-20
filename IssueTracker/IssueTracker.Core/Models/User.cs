namespace IssueTracker.Core.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Stringhe per la gestione sicura della password
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    // Relazioni: Un utente può aver creato molti ticket (se cliente) 
    // o può avere molti ticket assegnati (se developer)
    public ICollection<Ticket> CreatedTickets { get; set; } = new List<Ticket>();
    public ICollection<Ticket> AssignedTickets { get; set; } = new List<Ticket>();
}