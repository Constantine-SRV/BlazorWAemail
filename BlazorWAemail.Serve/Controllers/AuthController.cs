using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using BlazorWAemail.Server.Models;
using BlazorWAemail.Server.Services;
using BlazorWAemail.Shared;

namespace BlazorWAemail.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailSender _mail;
    private readonly IDictionary<string, string> _cfg;

    public AuthController(ApplicationDbContext db,
                          IEmailSender mail,
                          IDictionary<string, string> cfg)
    {
        _db  = db;
        _mail= mail;
        _cfg = cfg;
    }

    /* ---------- 1. e-mail → code --------------------------------------- */
    [HttpPost("sendcode")]
    public async Task<IActionResult> SendCode([FromBody] SendCodeRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Email))
            return BadRequest("Email required");

        var code = Random.Shared.Next(100_000, 999_999).ToString();
        var row = await _db.AuthCodes.FirstOrDefaultAsync(a => a.Email == r.Email);

        if (row is null)
            _db.AuthCodes.Add(new AuthCode
            {
                Email = r.Email,
                Code = code,
                Expiration = DateTime.UtcNow.AddMinutes(10)
            });
        else
        {
            row.Code       = code;
            row.Expiration = DateTime.UtcNow.AddMinutes(10);
        }
        await _db.SaveChangesAsync();

        await _mail.SendEmailAsync(r.Email, "Login code", $"Your code: {code}");
        Console.WriteLine($"code: {code}");
        return Ok();
    }

    /* ---------- 2. code+ JWT ------------------------------------------ */
    [HttpPost("verifycode")]
    public async Task<ActionResult<AuthResult>> Verify([FromBody] VerifyCodeRequest r)
    {
        var row = await _db.AuthCodes.FirstOrDefaultAsync(a => a.Email == r.Email);
        if (row is null || row.Code != r.Code || row.Expiration < DateTime.UtcNow)
            return Unauthorized();

        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == r.Email)
                   ?? (await _db.Users.AddAsync(new User { Email = r.Email })).Entity;

        /* --- issue token */
        var creds = new SigningCredentials(
                         new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["JwtSecretKey"])),
                         SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _cfg["JwtIssuer"],
            audience: _cfg["JwtAudience"],
            claims: new[] {
                new Claim(JwtRegisteredClaimNames.Sub,   user.Email),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier,     user.Id.ToString())
            },
            expires: DateTime.UtcNow.AddDays(int.Parse(_cfg["TokenExpirationDays"])),
            signingCredentials: creds);

        var encoded = new JwtSecurityTokenHandler().WriteToken(jwt);

        _db.UserTokens.Add(new UserToken
        {
            UserId = user.Id,
            Token  = encoded,
            CreatedAt  = DateTime.UtcNow,
            Expiration = jwt.ValidTo
        });
        await _db.SaveChangesAsync();

        return new AuthResult { Email = user.Email, Token = encoded };
    }

    /* ---------- 3. global logout -------------------------------------- */
    [Authorize]
    [HttpPost("logoutall")]
    public async Task<IActionResult> LogoutAll()
    {
        var email = User.Identity?.Name;
        if (email is null) return Forbid();

        var user = await _db.Users.Include(u => u.Tokens)
                                  .SingleOrDefaultAsync(u => u.Email == email);
        if (user is null) return NotFound();

        _db.UserTokens.RemoveRange(user.Tokens);
        await _db.SaveChangesAsync();
        return Ok();
    }
}
