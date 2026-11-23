using ControleGastos.Api.Json;
using ControleGastos.Core.Exceptions;
using Microsoft.AspNetCore.Http;
using Npgsql;

namespace ControleGastos.Api.Middleware;

/// <summary>Converte exceções em <c>{ error }</c> com o status HTTP adequado e loga os erros 500 no console.</summary>
public sealed partial class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public const string MensagemJsonInvalido = "JSON inválido";

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ControleGastosException e)
        {
            await EscreverAsync(context, e.StatusCode, e.Message, e.Extras);
        }
        catch (Exception e)
        {
            var mensagem = MensagemDe(e);
            LogErro(logger, e, context.Request.Method, context.Request.Path, mensagem);
            await EscreverAsync(context, StatusCodes.Status500InternalServerError, mensagem,
                new Dictionary<string, object>());
        }
    }

    /// <summary>Para erros do Postgres usa só o texto da mensagem (igual ao que o PostgREST repassava).</summary>
    public static string MensagemDe(Exception e) => e is PostgresException pg ? pg.MessageText : e.Message;

    private static Task EscreverAsync(
        HttpContext context, int status, string mensagem, IReadOnlyDictionary<string, object> extras)
    {
        var corpo = new Dictionary<string, object> { ["error"] = mensagem };
        foreach (var (chave, valor) in extras)
        {
            corpo[chave] = valor;
        }
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(corpo, JsonConfig.Padrao);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "{Metodo} {Caminho} error: {Mensagem}")]
    private static partial void LogErro(ILogger logger, Exception e, string metodo, string caminho, string mensagem);
}
