using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using BlazorWAemail.Server.Models;
using BlazorWAemail.Server.Services;
using BlazorWAemail.Shared;
using Microsoft.AspNetCore.Authorization;

namespace BlazorWAemail.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly IDictionary<string, string> _appSettings;

    public AuthController(
        ApplicationDbContext db,
        IEmailSender emailSender,
        IDictionary<string, string> appSettings)
    {
        _db = db;
        _emailSender = emailSender;
        _appSettings = appSettings;
    }

    [HttpPost("sendcode")]
    public async Task<IActionResult> SendCode([FromBody] SendCodeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("Email is required.");

        var code = new Random().Next(100000, 999999).ToString();
        var expiration = DateTime.UtcNow.AddMinutes(10);

        // Сохраняем или обновляем код
        var existing = await _db.AuthCodes.FirstOrDefaultAsync(x => x.Email == request.Email);
        if (existing == null)
        {
            _db.AuthCodes.Add(new AuthCode { Email = request.Email, Code = code, Expiration = expiration });
        }
        else
        {
            existing.Code = code;
            existing.Expiration = expiration;
        }

        await _db.SaveChangesAsync();

        try
        {
            await _emailSender.SendEmailAsync(
                request.Email,
                "Код подтверждения входа",
                $"Ваш код: {code}"
            );
            Console.WriteLine($"code: {code}");
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Ошибка при отправке e-mail: " + ex.Message);
        }

        return Ok();
    }
    [HttpPost("verifycode")]
    public async Task<ActionResult<AuthResult>> VerifyCode([FromBody] VerifyCodeRequest request)
    {
        Console.WriteLine("VerifyCode called");
        Console.WriteLine($"Email: {request.Email}, Code: {request.Code}");

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Code))
        {
            Console.WriteLine("Fail: Email or code is empty");
            return BadRequest("Email and code required.");
        }

        var codeRecord = await _db.AuthCodes.FirstOrDefaultAsync(x => x.Email == request.Email);

        if (codeRecord == null)
        {
            Console.WriteLine("Fail: Code record not found for this email");
            return Unauthorized();
        }

        if (codeRecord.Code != request.Code)
        {
            Console.WriteLine($"Fail: Code does not match (expected: {codeRecord.Code}, actual: {request.Code})");
            return Unauthorized();
        }

        if (codeRecord.Expiration < DateTime.UtcNow)
        {
            Console.WriteLine($"Fail: Code expired at {codeRecord.Expiration}, now {DateTime.UtcNow}");
            return Unauthorized();
        }

        // Try to find user by email
        var user = await _db.Users
            .Include(u => u.Tokens)
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(x => x.Email == request.Email);

        if (user == null)
        {
            Console.WriteLine("User not found, creating new user");
            user = new User
            {
                Email = request.Email,
                Tokens = new List<UserToken>(),
                UserRoles = new List<UserRole>()
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }
        else
        {
            Console.WriteLine($"User found: Id={user.Id}, Email={user.Email}");
        }

        // JWT settings from appSettings
        var secretKey = _appSettings["JwtSecretKey"];
        var issuer = _appSettings["JwtIssuer"];
        var audience = _appSettings["JwtAudience"];
        var tokenExpirationDays = int.Parse(_appSettings["TokenExpirationDays"]);
        var key = Encoding.UTF8.GetBytes(secretKey);

        var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new Claim(JwtRegisteredClaimNames.Email, user.Email)
        };


        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(tokenExpirationDays),
            signingCredentials: creds
        );

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        Console.WriteLine($"Success: Token generated for {user.Email}");

        return new AuthResult
        {
            Token = jwt,
            Email = user.Email
        };
    }

    [Authorize]
    [HttpPost("logoutall")]
    public async Task<IActionResult> LogoutAllDevices()
    {
        var email = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(email))
            return Forbid();

        var user = await _db.Users
            .Include(u => u.Tokens)
            .SingleOrDefaultAsync(u => u.Email == email);

        if (user == null) return NotFound();

        _db.UserTokens.RemoveRange(user.Tokens);
        await _db.SaveChangesAsync();

        return Ok();
    }


}
