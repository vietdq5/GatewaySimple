using Gateways.Middlewares;
using Gateways.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? ""))
        };
    });

builder.Services.AddAuthorization();

// Add Health Checks
builder.Services.AddHealthChecks();
// builder.Services.AddHealthChecks()
//     .AddCheck<HealthCheckService>("microservices_health");

// Add HttpClient factory
builder.Services.AddHttpClient();

// Add YARP
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint
app.MapHealthChecks("/health");
// app.MapHealthChecks("/health", new HealthCheckOptions
// {
//     ResponseWriter = async (context, report) =>
//     {
//         context.Response.ContentType = "application/json";

//         var response = new
//         {
//             Status = report.Status.ToString(),
//             Duration = report.TotalDuration,
//             Info = report.Entries.Select(e => new
//             {
//                 Key = e.Key,
//                 Status = e.Value.Status.ToString(),
//                 Description = e.Value.Description,
//                 Data = e.Value.Data
//             })
//         };

//         await JsonSerializer.SerializeAsync(
//             context.Response.Body,
//             response,
//             new JsonSerializerOptions { WriteIndented = true }
//         );
//     }
// });

// Map controllers
app.MapControllers();
app.UseMiddleware<RateLimitingMiddleware>();
// Map YARP routes
app.MapReverseProxy();

app.Run();