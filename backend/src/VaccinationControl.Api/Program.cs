using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using VaccinationControl.Api.Middleware;
using VaccinationControl.Api.OpenApi;
using VaccinationControl.Api.Security;
using VaccinationControl.Application;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Infrastructure;
using VaccinationControl.Infrastructure.Security;

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

// A interface é servida pelo Vite em outra origem, então toda chamada dela é cross-origin.
// As origens vêm da configuração porque mudam por ambiente — e não podem ser um curinga: a
// sessão vai trafegar em cookie, e credenciais exigem origem nomeada.
var corsSettings = builder.Configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>()
    ?? new CorsSettings();

builder.Services.AddCors(options =>
    options.AddPolicy(CorsSettings.PolicyName, policy => policy
        .WithOrigins(corsSettings.AllowedOrigins)
        .AllowCredentials()
        .AllowAnyHeader()
        .AllowAnyMethod()));

// A identidade do token alimenta os campos de auditoria das entidades gravadas.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// Leitura única das configurações de JWT: esta mesma instância assina os tokens, no
// JwtTokenGenerator, e valida os recebidos, aqui. Ler a chave em dois pontos independentes
// permitiria que divergissem — e o sintoma seria a API rejeitar o token que ela própria
// emitiu, sem nenhum erro que apontasse a causa.
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? new JwtSettings();

if (string.IsNullOrWhiteSpace(jwtSettings.Key))
{
    throw new InvalidOperationException(
        "A chave 'Jwt:Key' não foi configurada. Em desenvolvimento, defina com "
        + "'dotnet user-secrets set \"Jwt:Key\" \"<chave>\"'; em produção, por variável de ambiente.");
}

builder.Services.AddSingleton(Options.Create(jwtSettings));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Sem isto o handler renomeia 'sub' para ClaimTypes.NameIdentifier, e o CurrentUser
        // procuraria uma claim que deixou de existir — a auditoria ficaria sempre vazia.
        options.MapInboundClaims = false;

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // O token vem no cookie HttpOnly, não no cabeçalho Authorization. O handler do
                // JWT não olha cookie, então precisa ser copiado para o campo que ele espera.
                if (context.Request.Cookies.TryGetValue(AuthCookie.Name, out var token))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            }
        };

        // Configura a validação do token JWT recebido, para que o handler rejeite tokens
        // inválidos. A mesma instância de JwtSettings que assina os tokens é usada para
        // validá-los, garantindo que a API aceite apenas tokens que ela própria emitiu.
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
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
{
    // Um declara a credencial no catálogo do documento; o outro a exige em cada operação
    // protegida. Só o primeiro não basta: o catálogo sozinho não marca rota nenhuma.
    options.AddDocumentTransformer<SessionSecuritySchemeTransformer>();
    options.AddOperationTransformer<SessionSecurityRequirementTransformer>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    // O Scalar é a interface de teste da API. Ela não é parte do produto, então só aparece
    // em desenvolvimento. Em produção, a API é consumida pelo cliente Vite.
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(CorsSettings.PolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
