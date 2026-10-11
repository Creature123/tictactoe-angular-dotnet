using System.Text.Json.Serialization;
using TicTacToe.Api;
using TicTacToe.Api.Errors;

const string FrontendCorsPolicy = "Frontend";

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    // Enums travel as strings; numbers are rejected so "player": 1 is an INVALID_REQUEST (BE-11).
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)))
    // Actions check ModelState themselves, after the game lookup, so 404 wins over 400 (A-10).
    .ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true);

// Framework-generated errors (415, unmatched-route 404) get an errorCode too (BE-10).
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = ApiProblems.AddFrameworkErrorCode);
builder.Services.AddOpenApi();
builder.Services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy =>
    policy.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddTicTacToe();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Unexpected exceptions also become ProblemDetails (without internals).
app.UseExceptionHandler();
// Writes a ProblemDetails body for empty error responses, such as a 404 for a path that matches no route.
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseCors(FrontendCorsPolicy);
    app.MapOpenApi();
    // Interactive UI for reviewers; the document itself still comes from Microsoft.AspNetCore.OpenApi.
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "TicTacToe API v1");
        options.RoutePrefix = "swagger";
    });
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Exposes Program to WebApplicationFactory<Program> in TicTacToe.Api.Tests.
public partial class Program { }
