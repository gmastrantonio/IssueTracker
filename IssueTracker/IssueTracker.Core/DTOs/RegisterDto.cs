using System.ComponentModel.DataAnnotations;
using IssueTracker.Core.Models;

namespace IssueTracker.Core.DTOs;

public class RegisterDto
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6, ErrorMessage = "La password deve contenere almeno 6 caratteri")]
    public string Password { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; }
}