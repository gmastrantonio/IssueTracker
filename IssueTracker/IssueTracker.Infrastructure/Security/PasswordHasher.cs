using System.Security.Cryptography;
using IssueTracker.Core.Interfaces;

namespace IssueTracker.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16; // 128-bit
    private const int KeySize = 32;  // 256-bit
    private const int Iterations = 100000; // Numero di cicli di hashing
    private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;

    public string HashPassword(string password)
    {
        // 1. Genera un "Salt" casuale unico per questo utente
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

        using Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(password,salt, Iterations, HashAlgorithm);

        // 2. Genera l'hash della password usando il Salt e l'algoritmo PBKDF2
        byte[] hash = rfc2898DeriveBytes.GetBytes(KeySize);

        // 3. Unisce Salt e Hash in un'unica stringa testuale da salvare nel DB
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        // 1. Divide la stringa del DB per recuperare il Salt e l'Hash originale
        var parts = passwordHash.Split('.', 2);
        if (parts.Length != 2) return false;

        byte[] salt = Convert.FromBase64String(parts[0]);
        byte[] originalHash = Convert.FromBase64String(parts[1]);


        using Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithm);

        // 2. Calcola l'hash della password inserita usando lo STESSO salt
        byte[] newHash = rfc2898DeriveBytes.GetBytes(KeySize);

        // 3. Confronta i due array di byte in modo sicuro
        return CryptographicOperations.FixedTimeEquals(originalHash, newHash);
    }
}