using IssueTracker.Core.DTOs;
using IssueTracker.Core.Interfaces;
using IssueTracker.Core.Models;
using IssueTracker.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous] //Accessibile a chiunque senza Token
    public class AuthController : ControllerBase
    {

        private readonly DataContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;

        public AuthController(DataContext context, IPasswordHasher passwordHasher, ITokenService tokenService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<ActionResult<RegisterDto>> Register(RegisterDto dto)
        {
            // verify if the username or email already exists
            var user = await _context.Users.AnyAsync(x => x.Username == dto.Username);
            if (user)
                return BadRequest($"Utente {dto.Username} già presente!");
            // verify if the email already exists
            var email = await _context.Users.AnyAsync(x => x.Email == dto.Email);
            if (email)
                return BadRequest($"Email {dto.Email} già presente!");
            // hash the password
            var hashedPassword = _passwordHasher.HashPassword(dto.Password);
            // create a new user
            User newUser = new User
            {
                Username = dto.Username,
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = hashedPassword,
                Role = dto.Role
            };
            // add the new user to the database
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginDto>> Login(LoginDto dto)
        {
            // verify if the user exists
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Username == dto.Username);
            if (user == null)
                return Unauthorized($"Credenziali non valide!");
            // verify the password
            var isPasswordValid = _passwordHasher.VerifyPassword(dto.Password, user.PasswordHash);
            if (!isPasswordValid)
                return BadRequest($"Credenziali non valide!");

            // 3. Genera il token JWT ed il Refresh Token
            string token = _tokenService.CreateToken(user);
            // Salva il Refresh Token nel database con scadenza a 7 giorni
            var refreshToken = _tokenService.GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            // 4. Restituisce la risposta con il token
            return Ok(new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                Username = user.Username,
                Role = user.Role.ToString()
            });

        }

        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponseDto>> Refresh(RefreshTokenRequestDto dto)
        {
            // Cerca l'utente con il Refresh Token fornito
            var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == dto.RefreshToken);

            // Controlla se l'utente esiste o se il Refresh Token è scaduto
            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return Unauthorized("Refresh Token non valido o scaduto.");
            }

            // Genera una nuova coppia di Token (Token Rotation)
            var newAccessToken = _tokenService.CreateToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return Ok(new AuthResponseDto
            {
                Token = newAccessToken,
                RefreshToken = newRefreshToken,
                Username = user.Username,
                Role = user.Role.ToString()
            });
        }

        [HttpPost("revoke")]
        [Authorize]
        public async Task<IActionResult> Revoke()
        {
            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null) return BadRequest();

            // Annulla il token nel DB
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
