using LRS.Domain.Interfaces;
using LRS.Infrastructure.Persistence;
using LRS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using LRS.Application.Interfaces;
using LRS.Infrastructure.Services;
using LRS.API.Middleware;
using Microsoft.AspNetCore.Mvc;

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

// Add CORS to allow Angular frontend to access the API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Global Exception Handler Middleware (SRS requirement)
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// Enable CORS
app.UseCors("AllowAngularApp");

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
