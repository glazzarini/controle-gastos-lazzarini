using ControleGastos.Api.Configuracao;
using ControleGastos.Api.Health;
using ControleGastos.Api.Json;
using ControleGastos.Api.Middleware;
using ControleGastos.Core.Dtos.Responses;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(o => JsonConfig.Configurar(o.JsonSerializerOptions))
    .ConfigureApiBehaviorOptions(o =>
    {
        // Sem ProblemDetails: todo erro sai como { error } (contrato atual).
        o.SuppressMapClientErrors = true;
        o.InvalidModelStateResponseFactory =
            _ => new BadRequestObjectResult(new ErroResponse(ErrorHandlingMiddleware.MensagemJsonInvalido));
    });

builder.Services.AddControleGastos();

builder.Services
    .AddHealthChecks()
    .AddCheck<BancoHealthCheck>("banco", tags: ["ready"], timeout: TimeSpan.FromSeconds(3));

var app = builder.Build();

// Primeiro da pipeline: registra o status final, inclusive CORS, erros e 404/405.
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<CorsMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseStatusCodePages(RespostaStatusCode.EscreverAsync);

// Probes do Kubernetes — fora de /api para não alterar o contrato.
app.MapHealthChecks("/healthz", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/readyz", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") });

app.MapControllers();

await app.RunAsync();

/// <summary>Exposto para os testes de integração (WebApplicationFactory).</summary>
public partial class Program
{
}
