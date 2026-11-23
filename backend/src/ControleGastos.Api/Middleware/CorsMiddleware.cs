using Microsoft.AspNetCore.Http;

namespace ControleGastos.Api.Middleware;

/// <summary>
/// Reproduz o CORS das Vercel Functions: headers em toda resposta de <c>/api/*</c>,
/// <c>Allow-Methods</c> por rota e <c>OPTIONS</c> → 200 sem corpo.
/// </summary>
public sealed class CorsMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api", out var resto))
        {
            await next(context);
            return;
        }

        var headers = context.Response.Headers;
        headers.AccessControlAllowOrigin = "*";
        headers.AccessControlAllowMethods = MetodosPermitidos(resto);
        headers.AccessControlAllowHeaders = "Content-Type";

        if (HttpMethods.IsOptions(context.Request.Method))
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            return;
        }

        await next(context);
    }

    public static string MetodosPermitidos(PathString resto)
    {
        var segmentos = (resto.Value ?? string.Empty).Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segmentos switch
        {
            ["config"] => "GET, PUT, OPTIONS",
            [_] => "GET, POST, OPTIONS",
            [_, _] => "PUT, DELETE, OPTIONS",
            _ => "GET, POST, PUT, DELETE, OPTIONS",
        };
    }
}
