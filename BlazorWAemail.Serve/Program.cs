using BlazorWAemail.Server.Models;
using BlazorWAemail.Server.Services;
using Microsoft.EntityFrameworkCore;
using System.Text;
//using Microsoft.AspNetCore.Components.WebAssembly.Server;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


// Add AppSettingsService
builder.Services.AddScoped<AppSettingsService>();

// Load app settings from the database
var serviceProvider = builder.Services.BuildServiceProvider();
var appSettingsService = serviceProvider.GetRequiredService<AppSettingsService>();
var appSettings = appSettingsService.GetAppSettingsAsync().Result;
builder.Services.AddSingleton<IDictionary<string, string>>(appSettings);

var secretKey = appSettings["JwtSecretKey"];
var gptKey = appSettings["GptKey"];
var issuer = appSettings["JwtIssuer"];
var audience = appSettings["JwtAudience"];
var smtpServer = appSettings["SmtpServer"];
var smtpPort = int.Parse(appSettings["SmtpPort"]);
var smtpUser = appSettings["SmtpUser"];
var smtpPass = appSettings["SmtpPass"];
var key = Encoding.UTF8.GetBytes(secretKey);
var tokenExpirationDays = int.Parse(appSettings["TokenExpirationDays"]);
var AZURE_OPENAI_ENDPOINT = appSettings["AZURE_OPENAI_ENDPOINT"];
var AZURE_OPENAI_API_KEY = appSettings["AZURE_OPENAI_API_KEY"];

builder.Services.AddScoped<IEmailSender, GraphEmailSender>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");
app.Run();
