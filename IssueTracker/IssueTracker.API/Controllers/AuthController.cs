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

            // 3. Genera il token JWT
            string token = _tokenService.CreateToken(user);

            // 4. Restituisce la risposta con il token
            return Ok(new AuthResponseDto
            {
                Token = token,
                Username = user.Username,
                Role = user.Role.ToString()
            });

        }
    }
}
