using LRS.Domain.Interfaces;
using LRS.Infrastructure.Persistence;
using LRS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using LRS.Application.Interfaces;
using LRS.Infrastructure.Services;
using LRS.API.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using LRS.Domain.Entities;
using Fido2NetLib;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddLogging(); // Default logging

builder.Services.AddDbContext<LrsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ISourceRepository, SourceRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Configure Alfresco HTTP client using BaseUrl from configuration
builder.Services.AddHttpClient<IAlfrescoService, LRS.Infrastructure.Services.AlfrescoService>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["Alfresco:BaseUrl"]?.TrimEnd('/');
    if (!string.IsNullOrEmpty(baseUrl))
    {
        // Keep BaseAddress for convenience; AlfrescoService builds absolute URLs but this helps if relative URLs are used
        try
        {
            client.BaseAddress = new Uri(baseUrl);
        }
        catch
        {
            // ignore invalid URI here; AlfrescoService will still use configured string
        }
    }

    client.Timeout = TimeSpan.FromSeconds(100);
});

builder.Services.AddScoped<IDocumentService, LRS.Application.Services.DocumentService>();

builder.Services.AddMemoryCache();

builder.Services.AddSingleton<IFido2>(sp =>
{
    var origins = builder.Configuration.GetSection("fido2:origins").Get<HashSet<string>>()
                  ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    var config = new Fido2Configuration
    {
        ServerDomain = builder.Configuration["fido2:serverDomain"] ?? "localhost",
        ServerName = builder.Configuration["fido2:serverName"] ?? "LRS",
        Origins = origins,
        TimestampDriftTolerance = builder.Configuration.GetValue("fido2:timestampDriftTolerance", 300000)
    };
    return new Fido2(config);
});

// Add validation (SRS requirement)
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors)
                .Select(x => x.ErrorMessage)
                .ToList();
            
            return new BadRequestObjectResult(new
            {
                message = "Validation failed",
                errors = errors
            });
        };
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthorization();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Keep JWT claim names as issued (e.g. "role") so [Authorize(Roles = "...")] matches reliably.
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        RoleClaimType = "role"
    };
});

// Add CORS to allow Angular frontend to access the API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Seed one admin account for first-time login if configured.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<LrsDbContext>();
    var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("AdminSeeder");
    var seedSection = builder.Configuration.GetSection("SeedAdmin");

    var enabled = seedSection.GetValue<bool?>("Enabled") ?? true;
    var email = seedSection["Email"];
    var password = seedSection["Password"];

    if (enabled && !string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password))
    {
        var existingAdmin = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (existingAdmin is null)
        {
            dbContext.Users.Add(new AppUser
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = "Admin",
                RegisteredAt = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();
            logger.LogInformation("Seeded default admin account: {Email}", email);
        }
        else if (!string.Equals(existingAdmin.Role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            existingAdmin.Role = "Admin";
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Upgraded existing account to Admin role: {Email}", email);
        }
    }
}

// Global Exception Handler Middleware (SRS requirement)
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// Enable CORS
app.UseCors("AllowAngularApp");

app.UseSwagger();
app.UseSwaggerUI();

// Enable Authentication and Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
