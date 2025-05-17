using BlazorWAemail.Serve.Services;
using BlazorWAemail.Server.Models;
using BlazorWAemail.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add AppSettingsService
builder.Services.AddScoped<AppSettingsService>();

// Load app settings from the database (sync for setup, not recommended in production, but OK for setup)
var serviceProvider = builder.Services.BuildServiceProvider();
var appSettingsService = serviceProvider.GetRequiredService<AppSettingsService>();
var appSettings = appSettingsService.GetAppSettingsAsync().Result;
builder.Services.AddSingleton<IDictionary<string, string>>(appSettings);

// JWT setup
var secretKey = appSettings["JwtSecretKey"];
var issuer = appSettings["JwtIssuer"];
var audience = appSettings["JwtAudience"];
var key = Encoding.UTF8.GetBytes(secretKey);

// Authentication & Authorization
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            NameClaimType = ClaimTypes.Email
        };
     });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IEmailSender, GraphEmailSender>();
builder.Services.AddScoped<IUserRolesService, UserRolesService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ВАЖНО: Authentication раньше Authorization!
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");
app.Run();
