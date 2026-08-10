using System.Text.Json.Serialization;
using Scalar.AspNetCore;
using VaccinationControl.Api.Middleware;
using VaccinationControl.Application;
using VaccinationControl.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath);

builder.Services
    .AddControllers()
    // Enums trafegam como texto ("Dose", "BoosterDose"): um número cru no JSON obrigaria
    // o cliente a conhecer a numeração interna do domínio.
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

// Torna o host visível para o WebApplicationFactory dos testes de integração.
public partial class Program
{
}
