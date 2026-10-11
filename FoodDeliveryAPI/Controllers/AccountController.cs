
using FoodDeliveryAPI.Data;
using FoodDeliveryAPI.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace FoodDeliveryAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly FoodDeliveryDbContext _context;
    private readonly IConfiguration _configuration;

    public AccountController(
        FoodDeliveryDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public record RegisterDto(
        string FullName,
        string Email,
        string Password,
        string? Phone);

    public record LoginDto(string Email, string Password);

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName) ||
            string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest("Name, email and password are required.");

        if (dto.Password.Length < 8)
            return BadRequest("Password must be at least 8 characters.");

        var email = dto.Email.Trim();

        if (await _context.Users.AnyAsync(u => u.Email == email))
            return Conflict("Email already exists.");

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = email,
            PasswordHash = HashPassword(dto.Password),
            Phone = dto.Phone,
            Role = "Customer"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Registration successful.",
            user.UserId,
            user.FullName,
            user.Email,
            user.Role
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var email = dto.Email.Trim();
        var user = await _context.Users.FirstOrDefaultAsync(
            u => u.Email == email);

        if (user == null ||
            !VerifyPassword(dto.Password, user.PasswordHash))
            return Unauthorized("Invalid email or password.");

        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key is missing.");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.Trim())
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var minutes = int.TryParse(
            _configuration["Jwt:ExpiryMinutes"], out var value)
            ? value : 60;

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(minutes),
            signingCredentials: credentials);

        return Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt = token.ValidTo,
            user = new
            {
                user.UserId,
                user.FullName,
                user.Email,
                Role = user.Role.Trim()
            }
        });
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, 100000, HashAlgorithmName.SHA256, 32);

        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string stored)
    {
        var parts = stored.Split(':');
        if (parts.Length != 2)
            return false;

        try
        {
            var salt = Convert.FromBase64String(parts[0]);
            var expected = Convert.FromBase64String(parts[1]);

            var actual = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, 100000, HashAlgorithmName.SHA256,
                expected.Length);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
