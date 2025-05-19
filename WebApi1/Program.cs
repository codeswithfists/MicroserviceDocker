using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(o =>
{
    o.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "API 1",
            Version = "v1",
            Description = "API 1 description.",
            Contact = new OpenApiContact { Name = "Kevin Reid", Email = "kevinreid2023@outlook.com" },
            License = new OpenApiLicense { Name = "MIT", Url = new Uri("https://opensource.org/licenses/MIT") },
            TermsOfService = new Uri("https://opensource.org/licenses/MIT")
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes.Add("OAuth2", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows
            {
                Implicit = new OpenApiOAuthFlow
                {
                    Scopes = new Dictionary<string, string>
                    {
                        // this scope should contain a mapper to add the 'api1' client id to the audience
                        { "api1_scope", "api1_scope" }
                    }
                },
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri($"http://localhost:7080/realms/shadow/protocol/openid-connect/auth"),
                    TokenUrl = new Uri($"http://localhost:7080/realms/shadow/protocol/openid-connect/token"),
                    Scopes = new Dictionary<string, string>
                    {
                        { "api1_scope", "api1_scope" }
                    }
                },
                ClientCredentials = new OpenApiOAuthFlow
                {
                    TokenUrl = new Uri($"http://localhost:7080/realms/shadow/protocol/openid-connect/token"),
                    Scopes = new Dictionary<string, string>
                    {
                        { "api1_scope", "api1_scope" }
                    }
                }
            }
        });

        document.Components.SecuritySchemes.Add("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = JwtBearerDefaults.AuthenticationScheme
        });

        document.SecurityRequirements ??= [];
        document.SecurityRequirements.Add(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "OAuth2"
                    }
                }, []
            }
        });

        return Task.CompletedTask;
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt => builder.Configuration.Bind("JwtBearerOptions", opt));

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Admin", policy => policy.RequireRole("admin"))
    .AddPolicy("User", policy => policy.RequireRole("user"))
    .AddPolicy("Special", policy => policy.RequireRole("special1"));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();

app.MapGet("/hello", () => "Hello from Api 1");

app.MapGet("/admin/hello", () => "Hello securely from Api 1. You have the 'admin' role.").RequireAuthorization("Admin");

app.MapGet("/special", () => "You are special to Api 1").RequireAuthorization("Special");

app.MapGet("/divide/{num1}/{num2}", (int num1, int num2) =>
    {
        return num2 switch
        {
            0 => Results.BadRequest("Cannot divide by zero"),
            _ => Results.Ok(num1 / num2)
        };
    })
    .WithName("Divide")
    .WithDisplayName("Divide two numbers")
    .WithDescription("Performs the operation num1 / num2")
    .WithTags("Math")
    .Produces<int>(StatusCodes.Status200OK)
    .Produces<string>(StatusCodes.Status401Unauthorized)
    .ProducesValidationProblem(StatusCodes.Status400BadRequest)
    .RequireAuthorization("User");

app.MapGet("/claims", (ClaimsPrincipal principal) => principal.Claims.SelectMany(c => new[] { $"{c.Type}: {c.Value}" }))
    .RequireAuthorization();

app.Run();