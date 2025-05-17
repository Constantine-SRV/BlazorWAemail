using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BlazorWAemail.Server.Models;
using BlazorWAemail.Server.Services;
using BlazorWAemail.Serve.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

/* ---------- infrastructure ---------- */
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

/* ---------- settings from DB ---------- */
builder.Services.AddScoped<AppSettingsService>();
await using (var tmpScope = builder.Services.BuildServiceProvider().CreateAsyncScope())
{
    var cfg = await tmpScope.ServiceProvider
                            .GetRequiredService<AppSettingsService>()
                            .GetAppSettingsAsync();
    builder.Services.AddSingleton<IDictionary<string, string>>(cfg);
}

/* ---------- auth / JWT ---------- */
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        var cfg = opt.EventsType = typeof(BearerEvents);        
        var sp = builder.Services.BuildServiceProvider();
        var set = sp.GetRequiredService<IDictionary<string, string>>();

        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer    = set["JwtIssuer"],
            ValidAudience  = set["JwtAudience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                                   Encoding.UTF8.GetBytes(set["JwtSecretKey"])),

            NameClaimType  = ClaimTypes.Email               // User.Identity.Name == email
        };
    });


builder.Services.AddScoped<BearerEvents>();

/* ---------- DI ---------- */
builder.Services.AddAuthorization();
builder.Services.AddScoped<IEmailSender, GraphEmailSender>();
builder.Services.AddScoped<IUserRolesService, UserRolesService>();

/* ---------- pipeline ---------- */
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");
app.Run();

/* =======================================================================
 * JwtBearerEvents  –  check DB
 * =====================================================================*/
public sealed class BearerEvents : JwtBearerEvents
{
    private readonly ApplicationDbContext _db;
    public BearerEvents(ApplicationDbContext db) => _db = db;

    public override async Task TokenValidated(TokenValidatedContext ctx)
    {
        // "Bearer xxxxx.yyyyy.zzzzz"
        var header = ctx.Request.Headers["Authorization"].ToString();

        
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Fail("No bearer token");
            return;
        }

        var encoded = header["Bearer ".Length..];  

        
        var alive = await _db.UserTokens.AnyAsync(t => t.Token == encoded);

        if (!alive)
            ctx.Fail("Token revoked");
    }
}
