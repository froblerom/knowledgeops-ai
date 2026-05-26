using System.Text;
using KnowledgeOps.Api.Authorization;
using KnowledgeOps.Api.Controllers.Models;
using KnowledgeOps.Api.CurrentUser;
using KnowledgeOps.Api.Middleware;
using KnowledgeOps.Api.Observability;
using KnowledgeOps.Application;
using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Observability;
using KnowledgeOps.Infrastructure;
using KnowledgeOps.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024 + 8192;
});

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var correlation = context.HttpContext.RequestServices
                .GetRequiredService<ICorrelationContext>();
            var details = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .Select(entry => new ApiValidationItem(
                    entry.Key,
                    "The supplied value is invalid."))
                .ToArray();

            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(
                ApiErrorResponses.Create(
                    ApiErrorResponses.ValidationCode,
                    "One or more validation errors occurred.",
                    correlation.CorrelationId,
                    details));
        };
    });
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
builder.Services.AddScoped<ICorrelationContext, HttpCorrelationContext>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtSettings>>((jwtBearerOptions, jwtSettings) =>
    {
        var settings = jwtSettings.Value;
        jwtBearerOptions.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = settings.Issuer,
            ValidAudience = settings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
            ClockSkew = TimeSpan.Zero
        };
        jwtBearerOptions.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                if (context.Response.HasStarted)
                    return;

                context.HandleResponse();
                var correlation = context.HttpContext.RequestServices
                    .GetRequiredService<ICorrelationContext>();
                await ApiErrorResponses.WriteAsync(
                    context.HttpContext,
                    StatusCodes.Status401Unauthorized,
                    ApiErrorResponses.Unauthenticated(correlation.CorrelationId));
            },
            OnForbidden = async context =>
            {
                if (context.Response.HasStarted)
                    return;

                var correlation = context.HttpContext.RequestServices
                    .GetRequiredService<ICorrelationContext>();
                await ApiErrorResponses.WriteAsync(
                    context.HttpContext,
                    StatusCodes.Status403Forbidden,
                    ApiErrorResponses.Forbidden(correlation.CorrelationId));
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDev", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseCors("LocalDev");
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
