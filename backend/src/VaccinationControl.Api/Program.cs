using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using VaccinationControl.Api.Middleware;
using VaccinationControl.Api.OpenApi;
using VaccinationControl.Api.Security;
using VaccinationControl.Application;
using VaccinationControl.Application.Common.Interfaces;
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

// A identidade do token alimenta os campos de auditoria das entidades gravadas.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "A chave 'Jwt:Key' não foi configurada. Em desenvolvimento, defina com "
        + "'dotnet user-secrets set \"Jwt:Key\" \"<chave>\"'; em produção, por variável de ambiente.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Sem isto o handler renomeia 'sub' para ClaimTypes.NameIdentifier, e o CurrentUser
        // procuraria uma claim que deixou de existir — a auditoria ficaria sempre vazia.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            // Sem tolerância de relógio: o padrão de 5 minutos estende a validade do token.
            ClockSkew = TimeSpan.Zero
        };
    });

// Autenticação exigida por padrão; só o AuthController se declara [AllowAnonymous].
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    // A fallback policy exige token de todo endpoint, inclusive destes: sem AllowAnonymous
    // a própria documentação responderia 401 e não haveria como obter o token para usá-la.
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Torna o host visível para o WebApplicationFactory dos testes de integração.
public partial class Program
{
}
