using System.ComponentModel.DataAnnotations.Schema;

namespace IssueTracker.Core.Models;


//[Table("User")] // <--- Forza EF a cercare la tabella "User" al singolare
//corretto rinominando la tabella in "Users" al plurale, quindi non serve più il TableAttribute.
//Nel datacontext è stato definito DbSet<User> Users { get; set; } quindi EF capisce che la tabella si chiama Users
//cancellato il vecchio migration e creato un nuovo migration per aggiornare il database con la nuova tabella Users
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