using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseHttpsRedirection();
app.MapReverseProxy();

app.MapScalarApiReference(options =>
{
    // the OpenAPI documents to include. will be made available in a dropdown.
    options.AddDocument("api1", routePattern: "http://localhost:9000/api1/openapi/v1.json");
    options.AddDocument("api2", routePattern: "http://localhost:9000/api2/openapi/v1.json");

    // scalar doesn't seem to 'import' any servers added to the OpenAPI documents, so need to manually add them here.
    options.AddServer("http://localhost:9000/api1", "api1 server");
    options.AddServer("http://localhost:9000/api2", "api2 server");
});

app.Run();