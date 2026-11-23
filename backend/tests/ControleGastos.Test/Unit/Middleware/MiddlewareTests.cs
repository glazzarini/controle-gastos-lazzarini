using System.Text.Json;
using ControleGastos.Api.Middleware;
using ControleGastos.Core.Exceptions;
using ControleGastos.Test.Fixtures;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Npgsql;

namespace ControleGastos.Test.Unit.Middleware;

internal static class HttpContextFake
{
    public static DefaultHttpContext Criar(string metodo, string caminho)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = metodo;
        context.Request.Path = caminho;
        context.Response.Body = new MemoryStream();
        return context;
    }

    public static JsonElement Corpo(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return JsonDocument.Parse(context.Response.Body).RootElement.Clone();
    }
}

public sealed class CorsMiddlewareTests : TesteBase
{
    [Theory]
    [InlineData("/api/config", "GET, PUT, OPTIONS")]
    [InlineData("/api/lancamentos", "GET, POST, OPTIONS")]
    [InlineData("/api/categorias", "GET, POST, OPTIONS")]
    [InlineData("/api/contas-fixas", "GET, POST, OPTIONS")]
    [InlineData("/api/lancamentos/abc", "PUT, DELETE, OPTIONS")]
    [InlineData("/api/categorias/abc", "PUT, DELETE, OPTIONS")]
    [InlineData("/api/contas-fixas/abc/", "PUT, DELETE, OPTIONS")]
    [InlineData("/api", "GET, POST, PUT, DELETE, OPTIONS")]
    [InlineData("/api/a/b/c", "GET, POST, PUT, DELETE, OPTIONS")]
    public async Task RotasApi_RecebemHeadersEChamamProximo(string caminho, string metodos)
    {
        var chamou = false;
        var middleware = new CorsMiddleware(_ => { chamou = true; return Task.CompletedTask; });
        var context = HttpContextFake.Criar("GET", caminho);

        await middleware.InvokeAsync(context);

        Assert.True(chamou);
        Assert.Equal("*", context.Response.Headers.AccessControlAllowOrigin);
        Assert.Equal(metodos, context.Response.Headers.AccessControlAllowMethods);
        Assert.Equal("Content-Type", context.Response.Headers.AccessControlAllowHeaders);
    }

    [Fact]
    public async Task Options_Retorna200SemCorpoESemChamarProximo()
    {
        var chamou = false;
        var middleware = new CorsMiddleware(_ => { chamou = true; return Task.CompletedTask; });
        var context = HttpContextFake.Criar("OPTIONS", "/api/lancamentos");

        await middleware.InvokeAsync(context);

        Assert.False(chamou);
        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);
    }

    [Theory]
    [InlineData("/healthz")]
    [InlineData("/apix")]
    [InlineData("/")]
    public async Task ForaDeApi_NaoMexeNosHeaders(string caminho)
    {
        var chamou = false;
        var middleware = new CorsMiddleware(_ => { chamou = true; return Task.CompletedTask; });
        var context = HttpContextFake.Criar("OPTIONS", caminho);

        await middleware.InvokeAsync(context);

        Assert.True(chamou);
        Assert.False(context.Response.Headers.ContainsKey("Access-Control-Allow-Origin"));
    }
}

public sealed class ErrorHandlingMiddlewareTests : TesteBase
{
    private readonly ILogger<ErrorHandlingMiddleware> _logger = Substitute.For<ILogger<ErrorHandlingMiddleware>>();

    public ErrorHandlingMiddlewareTests() => _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

    private async Task<HttpContext> Executar(Exception? excecao)
    {
        var middleware = new ErrorHandlingMiddleware(
            _ => excecao is null ? Task.CompletedTask : throw excecao, _logger);
        var context = HttpContextFake.Criar("PUT", "/api/categorias/1");
        await middleware.InvokeAsync(context);
        return context;
    }

    [Fact]
    public async Task SemExcecao_NaoAlteraResposta()
    {
        var context = await Executar(null);
        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);
    }

    [Fact]
    public async Task Validacao_Retorna400ComError()
    {
        var context = await Executar(new ValidacaoException("nome é obrigatório"));

        Assert.Equal(400, context.Response.StatusCode);
        Assert.Equal("""{"error":"nome é obrigatório"}""", HttpContextFake.Corpo(context).GetRawText());
        Assert.DoesNotContain(_logger.ReceivedCalls(), c => c.GetMethodInfo().Name == "Log");
    }

    [Fact]
    public async Task NaoEncontrado_Retorna404()
    {
        var context = await Executar(new NaoEncontradoException("Categoria não encontrada"));
        Assert.Equal(404, context.Response.StatusCode);
        Assert.Equal("Categoria não encontrada", HttpContextFake.Corpo(context).GetProperty("error").GetString());
    }

    [Fact]
    public async Task Conflito_Retorna409ComCamposExtras()
    {
        var context = await Executar(new ConflitoException(
            "Não é possível excluir: 3 lançamento(s) usam essa categoria",
            new Dictionary<string, object> { ["lancamentos_em_uso"] = 3 }));

        Assert.Equal(409, context.Response.StatusCode);
        var corpo = HttpContextFake.Corpo(context);
        Assert.Equal("Não é possível excluir: 3 lançamento(s) usam essa categoria", corpo.GetProperty("error").GetString());
        Assert.Equal(3, corpo.GetProperty("lancamentos_em_uso").GetInt32());
    }

    [Fact]
    public async Task ErroInesperado_Retorna500ELogaNoConsole()
    {
        var context = await Executar(new InvalidOperationException("db down"));

        Assert.Equal(500, context.Response.StatusCode);
        Assert.Equal("""{"error":"db down"}""", HttpContextFake.Corpo(context).GetRawText());
        Assert.Single(_logger.ReceivedCalls(), c => c.GetMethodInfo().Name == "Log");
    }

    [Fact]
    public async Task ErroDoPostgres_UsaSoOTextoDaMensagem()
    {
        var pg = new PostgresException("invalid input syntax for type uuid: \"x\"", "ERROR", "ERROR", "22P02");

        var context = await Executar(pg);

        Assert.Equal(500, context.Response.StatusCode);
        Assert.Equal("invalid input syntax for type uuid: \"x\"", HttpContextFake.Corpo(context).GetProperty("error").GetString());
    }

    [Fact]
    public void MensagemDe()
    {
        Assert.Equal("a", ErrorHandlingMiddleware.MensagemDe(new InvalidOperationException("a")));
        Assert.Equal("b", ErrorHandlingMiddleware.MensagemDe(new PostgresException("b", "ERROR", "ERROR", "XX000")));
    }
}

public sealed class RespostaStatusCodeTests : TesteBase
{
    private static async Task<JsonElement> Executar(int status)
    {
        var context = HttpContextFake.Criar("PATCH", "/api/config");
        context.Response.StatusCode = status;
        await RespostaStatusCode.EscreverAsync(new StatusCodeContext(context, new StatusCodePagesOptions(), _ => Task.CompletedTask));
        Assert.StartsWith("application/json", context.Response.ContentType);
        return HttpContextFake.Corpo(context);
    }

    [Fact]
    public async Task Status405_UsaMensagemDoContratoAtual() =>
        Assert.Equal("Method not allowed", (await Executar(405)).GetProperty("error").GetString());

    [Fact]
    public async Task OutrosStatus_UsamReasonPhrase()
    {
        Assert.Equal("Not Found", (await Executar(404)).GetProperty("error").GetString());
        Assert.Equal("Unsupported Media Type", (await Executar(415)).GetProperty("error").GetString());
    }
}

public sealed class CorsMetodosPermitidosTests
{
    [Fact]
    public void PathStringSemValor_UsaTodosOsMetodos() =>
        Assert.Equal("GET, POST, PUT, DELETE, OPTIONS", CorsMiddleware.MetodosPermitidos(default));
}

/// <summary>Logger que guarda as mensagens já formatadas, para conferir o texto que vai para o console.</summary>
internal sealed class LoggerCaptura<T> : ILogger<T>
{
    public List<(LogLevel Nivel, string Mensagem)> Entradas { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter) => Entradas.Add((logLevel, formatter(state, exception)));
}

public sealed class RequestLoggingMiddlewareTests : TesteBase
{
    private readonly LoggerCaptura<RequestLoggingMiddleware> _logger = new();

    [Fact]
    public async Task LogaMetodoCaminhoQueryStatusEDuracao()
    {
        var middleware = new RequestLoggingMiddleware(c => { c.Response.StatusCode = 201; return Task.CompletedTask; }, _logger);
        var context = HttpContextFake.Criar("GET", "/api/lancamentos");
        context.Request.QueryString = new QueryString("?mes=2026-11");

        await middleware.InvokeAsync(context);

        var (nivel, mensagem) = Assert.Single(_logger.Entradas);
        Assert.Equal(LogLevel.Information, nivel);
        Assert.Matches(@"^GET /api/lancamentos\?mes=2026-11 -> 201 \(\d+ ms\)$", mensagem);
    }

    [Fact]
    public async Task SemQuery_NaoDeixaSobra()
    {
        var middleware = new RequestLoggingMiddleware(_ => Task.CompletedTask, _logger);

        await middleware.InvokeAsync(HttpContextFake.Criar("DELETE", "/api/categorias/1"));

        Assert.Matches(@"^DELETE /api/categorias/1 -> 200 \(\d+ ms\)$", Assert.Single(_logger.Entradas).Mensagem);
    }

    [Fact]
    public async Task ExcecaoQueEscapa_AindaLogaERepropaga()
    {
        var middleware = new RequestLoggingMiddleware(_ => throw new InvalidOperationException("x"), _logger);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(HttpContextFake.Criar("PUT", "/api/config")));

        Assert.StartsWith("PUT /api/config -> ", Assert.Single(_logger.Entradas).Mensagem);
    }

    [Theory]
    [InlineData("/healthz")]
    [InlineData("/readyz")]
    public async Task Probes_NaoSaoLogadosMasSeguemAPipeline(string caminho)
    {
        var chamou = false;
        var middleware = new RequestLoggingMiddleware(_ => { chamou = true; return Task.CompletedTask; }, _logger);

        await middleware.InvokeAsync(HttpContextFake.Criar("GET", caminho));

        Assert.True(chamou);
        Assert.Empty(_logger.Entradas);
    }

    [Theory]
    [InlineData("/healthz", true)]
    [InlineData("/READYZ", true)]
    [InlineData("/api/config", false)]
    [InlineData("/healthz/x", false)]
    public void Ignorar(string caminho, bool esperado) =>
        Assert.Equal(esperado, RequestLoggingMiddleware.Ignorar(new PathString(caminho)));
}
