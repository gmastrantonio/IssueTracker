namespace IssueTracker.Core.Interfaces;

public interface IPasswordHasher
{
    // Prende la password in chiaro e restituisce una stringa crittografata (l'hash)
    string HashPassword(string password);

    // Confronta una password in chiaro con l'hash salvato nel database
    bool VerifyPassword(string password, string passwordHash);
}