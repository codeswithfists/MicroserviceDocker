using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// add and configure reverse proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddRateLimiter(options =>
{
    // not sure why, but the default seems to be 503 Service Unavailable. 429 Too Many Requests is more correct.
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("customPolicy1", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromSeconds(10);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2;
    });

    options.AddTokenBucketLimiter("customPolicy2", opt =>
    {
        opt.TokenLimit = 10;
        opt.TokensPerPeriod = 5;
        opt.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
    });
});

// add and configure authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt => builder.Configuration.Bind("JwtBearerOptions", opt));

// add authorization, with an Admin policy for the 'api1-admin' route
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Admin", policy => policy.RequireRole("admin"));

// need to configure CORS to allow the Blazor app to call this API
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(builder => builder
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()
    )
);

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapReverseProxy();

app.MapScalarApiReference(options =>
{
    options.AddDocument("api1", routePattern: "https://localhost:7007/api1/openapi/v1.json"); // Docker: http://localhost:9000/api1/openapi/v1.json
    options.AddDocument("api2", routePattern: "https://localhost:7007/api2/openapi/v1.json");

    // override the servers, so that we go via the reverse proxy instead of the actual server
    options.Servers =
    [
        new ScalarServer("https://localhost:7007/api2", "API 2 server"), // Docker: http://localhost:9000/api2
        new ScalarServer("https://localhost:7007/api1", "API 1 server")
    ];

    options.WithPreferredScheme("OAuth2")

        /* Implicit flow returns an ID token to the app, either in the URL fragment or preferably as a
         * Form Post. This flow does not require the presence of a client secret.
         * 
         * Requires 'Implicit flow' authentication flow to be enabled for this client in KeyCloak.
         * 
         * NOTE: You should use this flow for login-only use cases; if you need to request Access Tokens while
         * logging the user in so you can call an API, use Authorization Code Flow with PKCE or Hybrid Flow.
         */
        .AddImplicitFlow("OAuth2", flow =>
        {
            flow.AuthorizationUrl = $"{builder.Configuration["JwtBearerOptions:Authority"]}/protocol/openid-connect/auth";
            flow.ClientId = builder.Configuration["JwtBearerOptions:ClientId"];
        })

        /* The Resource Owner Password Flow requests that users provide credentials (eg. username and password),
         * typically using an interactive form. Because credentials are sent to the backend and can be stored for
         * future use before being exchanged for an Access Token, it is imperative that the application is
         * absolutely trusted with this information. Not really recommended.
         * 
         * Requires 'Direct access grants' authentication flow to be enabled for this client in KeyCloak.
         */
        .AddPasswordFlow("OAuth2", flow =>
        {
            flow.TokenUrl = $"{builder.Configuration["JwtBearerOptions:Authority"]}/protocol/openid-connect/token";
            flow.ClientId = builder.Configuration["JwtBearerOptions:ClientId"];
            flow.ClientSecret = builder.Configuration["JwtBearerOptions:ClientSecret"];
            flow.Username = "boss";
            flow.Password = "123";
        })

        /* The Client Credentials Flow involves an application exchanging its application credentials, such as
         * client ID and client secret, for an access token. This flow is best suited for Machine-to-Machine
         * (M2M) applications, such as CLIs, daemons, or backend services, because the system must authenticate
         * and authorize the application instead of a user.
         * 
         * Requires 'Service accounts roles' authentication flow to be enabled for this client in KeyCloak.
         */
        .AddClientCredentialsFlow("OAuth2", flow =>
        {
            flow.TokenUrl = $"{builder.Configuration["JwtBearerOptions:Authority"]}/protocol/openid-connect/token";
            flow.ClientId = builder.Configuration["JwtBearerOptions:ClientId"];
            flow.ClientSecret = builder.Configuration["JwtBearerOptions:ClientSecret"];
        })

        /* The Authorization Code Flow involves exchanging an authorization code for a token. This flow can
         * only be used for confidential applications (such as Regular Web Applications) because the
         * application's authentication methods are included in the exchange and must be kept secure.
         * 
         * PREFERRED.
         * 
         * Requires 'Standard flow' authentication flow to be enabled for this client in KeyCloak.
         */
        .AddAuthorizationCodeFlow("OAuth2", flow =>
        {
            flow.AuthorizationUrl = $"{builder.Configuration["JwtBearerOptions:Authority"]}/protocol/openid-connect/auth";
            flow.TokenUrl = $"{builder.Configuration["JwtBearerOptions:Authority"]}/protocol/openid-connect/token";
            flow.ClientId = builder.Configuration["JwtBearerOptions:ClientId"];
            flow.ClientSecret = builder.Configuration["JwtBearerOptions:ClientSecret"];
            flow.Pkce = Pkce.Sha256;
        })

        // option to to supply your own authentication token
        .AddHttpAuthentication("BearerAuth", _ => { });

    options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

app.Run();