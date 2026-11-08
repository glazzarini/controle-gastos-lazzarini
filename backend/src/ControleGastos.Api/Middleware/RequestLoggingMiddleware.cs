using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace ControleGastos.Api.Middleware;

/// <summary>
/// Loga uma linha por requisição no console (método, caminho, query, status e duração).
/// Não loga corpo nem headers. Ignora os probes do Kubernetes (<c>/healthz</c>, <c>/readyz</c>),
/// que rodam a cada poucos segundos e só gerariam ruído.
/// </summary>
public sealed partial class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    private static readonly PathString[] CaminhosIgnorados = [new("/healthz"), new("/readyz")];

    public async Task InvokeAsync(HttpContext context)
    {
        if (Ignorar(context.Request.Path))
        {
            await next(context);
            return;
        }

        var inicio = Stopwatch.GetTimestamp();
        try
        {
            await next(context);
        }
        finally
        {
            var duracao = (long)Stopwatch.GetElapsedTime(inicio).TotalMilliseconds;
            LogRequisicao(logger, context.Request.Method, context.Request.Path, context.Request.QueryString,
                context.Response.StatusCode, duracao);
        }
    }

    public static bool Ignorar(PathString caminho) => Array.Exists(CaminhosIgnorados, c => caminho.Equals(c));

    [LoggerMessage(Level = LogLevel.Information, Message = "{Metodo} {Caminho}{Query} -> {Status} ({DuracaoMs} ms)")]
    private static partial void LogRequisicao(
        ILogger logger, string metodo, PathString caminho, QueryString query, int status, long duracaoMs);
}
