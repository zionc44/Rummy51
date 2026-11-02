using Microsoft.AspNetCore.Mvc;
using Rummy51.Infrastructure;
using Rummy51.Server.Authentication;
using Rummy51.Server.Errors;
using Rummy51.Server.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCookieAuthentication();
builder.Services.AddSignalR();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Model binding rejects a request before any of our code runs, so its reply is
// reshaped here into the same payload the rest of the API returns. Each rule in
// the request types carries a code as its ErrorMessage, which is what surfaces.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context => ApiProblem.Result(
        context.HttpContext,
        StatusCodes.Status400BadRequest,
        "ValidationFailed",
        ModelStateErrors.From(context.ModelState));
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

app.UseDefaultFiles();
app.MapStaticAssets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // AddOpenApi/MapOpenApi generate the document; Swashbuckle only renders it.
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Rummy51 API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<LobbyHub>(LobbyHub.Route);

app.MapFallbackToFile("/index.html");

app.Run();
