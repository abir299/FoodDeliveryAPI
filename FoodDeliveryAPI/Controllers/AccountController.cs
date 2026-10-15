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
public class AccountController(
    FoodDeliveryDbContext context,
    IConfiguration configuration) : ControllerBase
{
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
        {
            return BadRequest(
                "Name, email and password are required.");
        }

        var fullName = dto.FullName.Trim();
        var email = dto.Email.Trim().ToLowerInvariant();

        if (fullName.Length > 150 || email.Length > 256)
            return BadRequest("Name or email is too long.");

        if (!IsValidEmail(email))
            return BadRequest("A valid email address is required.");

        if (dto.Password.Length < 8)
            return BadRequest(
                "Password must be at least 8 characters.");

        if (dto.Password.Length > 128)
            return BadRequest("Password is too long.");

        if (dto.Phone?.Length > 30)
            return BadRequest("Phone number is too long.");

        if (await context.Users.AnyAsync(u => u.Email == email))
            return Conflict("Email already exists.");

        var user = new User
        {
            FullName = fullName,
            Email = email,
            PasswordHash = HashPassword(dto.Password),
            Phone = string.IsNullOrWhiteSpace(dto.Phone)
                ? null
                : dto.Phone.Trim(),
            Role = "Customer"
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return Ok(new
        {
            message = "Registration successful.",
            user.UserId,
            user.FullName,
            user.Email,
            Role = user.Role.Trim()
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrEmpty(dto.Password))
        {
            return BadRequest("Email and password are required.");
        }

        var email = dto.Email.Trim().ToLowerInvariant();

        var user = await context.Users.FirstOrDefaultAsync(
            u => u.Email == email);

        if (user == null ||
            !VerifyPassword(dto.Password, user.PasswordHash))
        {
            return Unauthorized("Invalid email or password.");
        }

        var key = configuration["Jwt:Key"];
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];

        if (string.IsNullOrWhiteSpace(key) ||
            Encoding.UTF8.GetByteCount(key) < 32 ||
            string.IsNullOrWhiteSpace(issuer) ||
            string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException(
                "JWT configuration is missing or invalid. " +
                "Configure Jwt:Key (at least 32 UTF-8 bytes), " +
                "Jwt:Issuer and Jwt:Audience.");
        }

        var minutes = int.TryParse(
            configuration["Jwt:ExpiryMinutes"], out var configuredMinutes)
            && configuredMinutes > 0
            ? configuredMinutes
            : 60;

        var role = user.Role.Trim();

        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, role)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(minutes),
            signingCredentials: credentials);

        return Ok(new
        {
            token = new JwtSecurityTokenHandler()
                .WriteToken(token),
            expiresAt = token.ValidTo,
            user = new
            {
                user.UserId,
                user.FullName,
                user.Email,
                Role = role
            }
        });
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var address = new System.Net.Mail.MailAddress(email);
            return address.Address == email;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            100000,
            HashAlgorithmName.SHA256,
            32);

        return $"{Convert.ToBase64String(salt)}:" +
               $"{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(
        string password, string stored)
    {
        var parts = stored.Split(':');

        if (parts.Length != 2)
            return false;

        try
        {
            var salt = Convert.FromBase64String(parts[0]);
            var expected = Convert.FromBase64String(parts[1]);

            if (salt.Length != 16 || expected.Length != 32)
                return false;

            var actual = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                100000,
                HashAlgorithmName.SHA256,
                expected.Length);

            return CryptographicOperations.FixedTimeEquals(
                actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
