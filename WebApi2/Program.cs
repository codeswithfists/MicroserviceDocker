using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt => builder.Configuration.Bind("JwtBearerOptions", opt));

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Admin", policy => policy.RequireRole("admin"))
    .AddPolicy("User", policy => policy.RequireRole("user"));

builder.Services.AddOpenApi(o =>
{
    o.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "API 2",
            Version = "v1",
            Description = "API 2 description.",
            Contact = new OpenApiContact { Name = "Kevin Reid", Email = "kevin@email.com" },
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
                        { "api2_scope", "api2_scope" }
                    }
                }
            }
        });

        return Task.CompletedTask;
    });
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();

app.MapGet("/hello", () => "Hello from Api 2");

app.MapGet("/user", () => "User endpoint in Api 2").RequireAuthorization("User");

app.MapGet("/admin", () => "Admin endpoint in Api 2").RequireAuthorization("Admin");

app.MapGet("/claims", (ClaimsPrincipal principal) => principal.Claims.SelectMany(c => new[] { $"{c.Type}: {c.Value}" }))
    .WithName("Claims")
    .WithDisplayName("Current user claims")
    .WithDescription("Get a list of the claims of the current user")
    .WithTags("Auth")
    .Produces<string[]>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status401Unauthorized)
    .RequireAuthorization();

app.Run();