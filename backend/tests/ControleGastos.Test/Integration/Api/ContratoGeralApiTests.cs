using System.Text.Json;
using ControleGastos.Test.Fixtures;

namespace ControleGastos.Test.Integration.Api;

/// <summary>Regras transversais do contrato: CORS, JSON, erros, health checks.</summary>
public sealed class ContratoGeralApiTests(PostgresFixture db) : ApiTesteBase(db)
{
    [Theory]
    [InlineData("/api/lancamentos", "GET, POST, OPTIONS")]
    [InlineData("/api/categorias/x", "PUT, DELETE, OPTIONS")]
    [InlineData("/api/config", "GET, PUT, OPTIONS")]
    public async Task Cors_EmTodaRespostaDaApi_InclusiveErros(string url, string metodos)
    {
        var r = await Enviar(HttpMethod.Patch, url, "{}");

        Assert.Equal(405, r.Codigo);
        Assert.Equal("*", Assert.Single(r.Mensagem.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Equal(metodos, string.Join(", ", r.Mensagem.Headers.GetValues("Access-Control-Allow-Methods")));
        Assert.Equal("Content-Type", Assert.Single(r.Mensagem.Headers.GetValues("Access-Control-Allow-Headers")));
    }

    [Fact]
    public async Task Respostas_SaoJsonUtf8ComAcentosLiterais()
    {
        var r = await Post("/api/lancamentos", "{}");

        Assert.Equal("application/json", r.Mensagem.Content.Headers.ContentType!.MediaType);
        Assert.Contains("são obrigatórios", r.Texto, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Lancamento_FormatoCompletoDoJson()
    {
        await Db.InserirLancamentoAsync("2026-06-05", "Pet", 89.90m, "Almoço", null, "A vista",
            new DateTime(2026, 6, 1, 14, 30, 0, DateTimeKind.Utc));

        var item = (await Get("/api/lancamentos")).Corpo[0];

        Assert.Equal(["id", "data", "descricao", "categoria", "valor", "observacao", "tipo", "created_at"],
            item.EnumerateObject().Select(p => p.Name));
        Assert.Equal(JsonValueKind.Null, item.GetProperty("observacao").ValueKind);
        Assert.Equal("2026-06-01T14:30:00+00:00", item.GetProperty("created_at").GetString());
        Assert.Equal("89.9", item.GetProperty("valor").GetRawText());
    }

    [Fact]
    public async Task JsonInvalido_Retorna400()
    {
        var r = await Post("/api/categorias", "{nome:");
        Assert.Equal(400, r.Codigo);
        Assert.Equal("JSON inválido", r.Erro);
    }

    [Fact]
    public async Task JsonQueNaoEObjeto_Retorna400() =>
        Assert.Equal("JSON inválido", (await Post("/api/categorias", "[1,2]")).Erro);

    [Fact]
    public async Task BodyAusente_CaiNasValidacoes()
    {
        var r = await Enviar(HttpMethod.Post, "/api/contas-fixas", json: null);
        Assert.Equal(400, r.Codigo);
        Assert.Equal("nome é obrigatório", r.Erro);
    }

    [Fact]
    public async Task BodyNullLiteral_CaiNasValidacoes() =>
        Assert.Equal("Informe renda_guilherme e/ou renda_esposa", (await Put("/api/config", "null")).Erro);

    [Fact]
    public async Task ContentTypeNaoJson_Retorna415ComError()
    {
        var r = await Enviar(HttpMethod.Post, "/api/categorias", "nome=x", contentType: "text/plain");
        Assert.Equal(415, r.Codigo);
        Assert.Equal("Unsupported Media Type", r.Erro);
    }

    [Fact]
    public async Task RotaInexistente_Retorna404ComError()
    {
        var r = await Get("/api/nao-existe");
        Assert.Equal(404, r.Codigo);
        Assert.Equal("Not Found", r.Erro);
    }

    [Fact]
    public async Task BancoInacessivel_Retorna500ComError()
    {
        var r = await Enviar(HttpMethod.Get, "/api/categorias", semBanco: true);
        Assert.Equal(500, r.Codigo);
        Assert.False(string.IsNullOrWhiteSpace(r.Erro));
    }

    [Fact]
    public async Task Healthz_SempreSaudavel_MesmoSemBanco()
    {
        Assert.Equal((200, "Healthy"), ((await Get("/healthz")).Codigo, (await Get("/healthz")).Texto));
        var semBanco = await Enviar(HttpMethod.Get, "/healthz", semBanco: true);
        Assert.Equal(200, semBanco.Codigo);
        Assert.False(semBanco.Mensagem.Headers.Contains("Access-Control-Allow-Origin")); // fora de /api
    }

    [Fact]
    public async Task Readyz_DependeDoBanco()
    {
        var ok = await Get("/readyz");
        Assert.Equal((200, "Healthy"), (ok.Codigo, ok.Texto));

        var semBanco = await Enviar(HttpMethod.Get, "/readyz", semBanco: true);
        Assert.Equal((503, "Unhealthy"), (semBanco.Codigo, semBanco.Texto));
    }
}
