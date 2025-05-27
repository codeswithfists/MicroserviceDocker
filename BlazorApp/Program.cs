using BlazorApp;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddHttpClient("WebApi1", client => client.BaseAddress = new Uri("https://localhost:7007/api1/"))
    .AddHttpMessageHandler(sp => sp.GetRequiredService<AuthorizationMessageHandler>()
        .ConfigureHandler(authorizedUrls: ["https://localhost:7007/api1"]));

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("WebApi1"));

builder.Services.AddOidcAuthentication(options =>
{
    options.ProviderOptions.Authority = "http://localhost:7081/realms/shadow";
    options.ProviderOptions.ClientId = "blazorapp";
    options.ProviderOptions.ResponseType = "code"; // the OAuth2 flow to use.  Authorization Code is recommended.
    options.ProviderOptions.DefaultScopes.Add("api1_scope");
    options.ProviderOptions.DefaultScopes.Add("api2_scope");
});

await builder.Build().RunAsync();
