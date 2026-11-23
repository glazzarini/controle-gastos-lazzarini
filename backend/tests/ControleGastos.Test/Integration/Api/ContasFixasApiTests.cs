using ControleGastos.Test.Fixtures;

namespace ControleGastos.Test.Integration.Api;

/// <summary>Porte de tests/api/contas-fixas.test.js e tests/api/contas-fixas-id.test.js.</summary>
public sealed class ContasFixasApiTests(PostgresFixture db) : ApiTesteBase(db)
{
    private const string Js = "contas-fixas.test.js";
    private const string JsId = "contas-fixas-id.test.js";

    [Fact]
    [Trait("Vitest", Js + " › OPTIONS retorna 200 e encerra")]
    public async Task Options_Retorna200() => Assert.Equal(200, (await Enviar(HttpMethod.Options, "/api/contas-fixas")).Codigo);

    [Fact]
    [Trait("Vitest", Js + " › GET lista contas fixas com valor numérico")]
    public async Task Get_ListaComValorNumerico()
    {
        await Db.InserirContaFixaAsync("Condomínio", 1600, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        await Db.InserirContaFixaAsync("Vivo", 124, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));

        var r = await Get("/api/contas-fixas");

        Assert.Equal(200, r.Codigo);
        Assert.Equal(["Condomínio", "Vivo"], r.Corpo.EnumerateArray().Select(c => c.GetProperty("nome").GetString()));
        AssertNumero(1600, r.Corpo[0].GetProperty("valor"));
        Assert.Equal("2026-01-01T00:00:00+00:00", r.Corpo[0].GetProperty("created_at").GetString());
    }

    [Fact]
    [Trait("Vitest", Js + " › GET retorna 500 em erro do supabase")]
    public async Task Get_ErroDoBanco_Retorna500()
    {
        await using (await Db.TabelaIndisponivelAsync("contas_fixas"))
        {
            Assert.Equal(500, (await Get("/api/contas-fixas")).Codigo);
        }
    }

    [Fact]
    [Trait("Vitest", Js + " › POST cria conta fixa válida")]
    public async Task Post_Cria()
    {
        var r = await Post("/api/contas-fixas", """{"nome":"  Internet  ","valor":120}""");

        Assert.Equal(201, r.Codigo);
        Assert.Equal("Internet", r.Corpo.GetProperty("nome").GetString());
        AssertNumero(120, r.Corpo.GetProperty("valor"));
    }

    [Theory]
    [Trait("Vitest", Js + " › POST rejeita nome vazio (string em branco)")]
    [Trait("Vitest", Js + " › POST rejeita nome ausente no body")]
    [InlineData("""{"nome":"  ","valor":10}""")]
    [InlineData("""{"valor":10}""")]
    public async Task Post_NomeVazio_Retorna400(string json)
    {
        var r = await Post("/api/contas-fixas", json);
        Assert.Equal(400, r.Codigo);
        Assert.Equal("nome é obrigatório", r.Erro);
    }

    [Theory]
    [Trait("Vitest", Js + " › POST rejeita valor inválido (NaN)")]
    [Trait("Vitest", Js + " › POST rejeita valor negativo")]
    [InlineData("""{"nome":"X","valor":"abc"}""")]
    [InlineData("""{"nome":"X","valor":-1}""")]
    [InlineData("""{"nome":"X"}""")]
    public async Task Post_ValorInvalido_Retorna400(string json)
    {
        var r = await Post("/api/contas-fixas", json);
        Assert.Equal(400, r.Codigo);
        Assert.Equal("valor deve ser um número maior ou igual a 0", r.Erro);
    }

    [Fact]
    [Trait("Vitest", Js + " › POST retorna 500 em erro do supabase")]
    public async Task Post_ErroDoBanco_Retorna500() =>
        Assert.Equal("numeric field overflow", (await Post("/api/contas-fixas", """{"nome":"X","valor":100000000000}""")).Erro);

    [Fact]
    [Trait("Vitest", Js + " › método não suportado retorna 405")]
    public async Task MetodoNaoSuportado_Retorna405() =>
        Assert.Equal(405, (await Enviar(HttpMethod.Patch, "/api/contas-fixas", "{}")).Codigo);

    // ── contas-fixas-id.test.js ───────────────────────────────────────────

    [Fact]
    [Trait("Vitest", JsId + " › OPTIONS retorna 200 e encerra")]
    public async Task Id_Options_Retorna200() => Assert.Equal(200, (await Enviar(HttpMethod.Options, "/api/contas-fixas/1")).Codigo);

    [Fact]
    [Trait("Vitest", JsId + " › sem id retorna 400")]
    public async Task Id_SemId_NaoChegaNaRotaDeId() => Assert.Equal(405, (await Put("/api/contas-fixas/", "{}")).Codigo);

    [Fact]
    [Trait("Vitest", JsId + " › PUT atualiza nome e valor")]
    public async Task Id_Put_NomeEValor()
    {
        var id = await Db.InserirContaFixaAsync("Net", 100);

        var r = await Put($"/api/contas-fixas/{id}", """{"nome":"Internet","valor":150}""");

        Assert.Equal(200, r.Codigo);
        Assert.True(r.Corpo.GetProperty("updated").GetBoolean());
        var cf = r.Corpo.GetProperty("conta_fixa");
        Assert.Equal(id, cf.GetProperty("id").GetGuid());
        Assert.Equal("Internet", cf.GetProperty("nome").GetString());
        AssertNumero(150, cf.GetProperty("valor"));
    }

    [Fact]
    [Trait("Vitest", JsId + " › PUT atualiza só o nome")]
    public async Task Id_Put_SoNome()
    {
        var id = await Db.InserirContaFixaAsync("Net", 100);
        var cf = (await Put($"/api/contas-fixas/{id}", """{"nome":"Novo Nome"}""")).Corpo.GetProperty("conta_fixa");
        Assert.Equal("Novo Nome", cf.GetProperty("nome").GetString());
        AssertNumero(100, cf.GetProperty("valor"));
    }

    [Fact]
    [Trait("Vitest", JsId + " › PUT atualiza só o valor")]
    public async Task Id_Put_SoValor()
    {
        var id = await Db.InserirContaFixaAsync("X", 100);
        var cf = (await Put($"/api/contas-fixas/{id}", """{"valor":200}""")).Corpo.GetProperty("conta_fixa");
        Assert.Equal("X", cf.GetProperty("nome").GetString());
        AssertNumero(200, cf.GetProperty("valor"));
    }

    [Theory]
    [Trait("Vitest", JsId + " › PUT rejeita nome vazio (string em branco)")]
    [Trait("Vitest", JsId + " › PUT rejeita nome enviado como string vazia")]
    [InlineData("""{"nome":"   "}""")]
    [InlineData("""{"nome":""}""")]
    public async Task Id_Put_NomeVazio_Retorna400(string json)
    {
        var r = await Put($"/api/contas-fixas/{Guid.NewGuid()}", json);
        Assert.Equal(400, r.Codigo);
        Assert.Equal("nome não pode ser vazio", r.Erro);
    }

    [Theory]
    [Trait("Vitest", JsId + " › PUT rejeita valor inválido")]
    [Trait("Vitest", JsId + " › PUT rejeita valor negativo")]
    [InlineData("""{"valor":"abc"}""")]
    [InlineData("""{"valor":-1}""")]
    public async Task Id_Put_ValorInvalido_Retorna400(string json)
    {
        var r = await Put($"/api/contas-fixas/{Guid.NewGuid()}", json);
        Assert.Equal(400, r.Codigo);
        Assert.Equal("valor deve ser um número maior ou igual a 0", r.Erro);
    }

    [Fact]
    [Trait("Vitest", JsId + " › PUT retorna 404 quando conta fixa não existe")]
    public async Task Id_Put_Inexistente_Retorna404()
    {
        var r = await Put($"/api/contas-fixas/{Guid.NewGuid()}", """{"valor":10}""");
        Assert.Equal(404, r.Codigo);
        Assert.Equal("Conta fixa não encontrada", r.Erro);
    }

    [Fact]
    [Trait("Vitest", JsId + " › PUT retorna 500 em outro erro do supabase")]
    public async Task Id_Put_OutroErro_Retorna500()
    {
        var r = await Put("/api/contas-fixas/nao-e-uuid", """{"valor":10}""");
        Assert.Equal(500, r.Codigo);
        Assert.Equal("invalid input syntax for type uuid: \"nao-e-uuid\"", r.Erro);
    }

    [Fact]
    [Trait("Vitest", JsId + " › DELETE exclui conta fixa")]
    public async Task Id_Delete_Exclui()
    {
        var id = await Db.InserirContaFixaAsync("X", 1);
        var r = await Delete($"/api/contas-fixas/{id}");
        Assert.Equal("""{"deleted":true}""", r.Texto);
        Assert.Equal(0L, await Db.ConsultarUmAsync<long>("SELECT COUNT(*) FROM contas_fixas"));
    }

    [Fact]
    [Trait("Vitest", JsId + " › DELETE retorna 500 em erro do supabase")]
    public async Task Id_Delete_ErroDoBanco_Retorna500() =>
        Assert.Equal(500, (await Delete("/api/contas-fixas/nao-e-uuid")).Codigo);

    [Fact]
    [Trait("Vitest", JsId + " › método não suportado retorna 405")]
    public async Task Id_MetodoNaoSuportado_Retorna405() =>
        Assert.Equal(405, (await Enviar(HttpMethod.Patch, "/api/contas-fixas/1", "{}")).Codigo);

    [Fact]
    public async Task Id_Put_SemCampos_DevolveAtualOu404()
    {
        var id = await Db.InserirContaFixaAsync("X", 1);
        Assert.Equal(200, (await Put($"/api/contas-fixas/{id}", "{}")).Codigo);
        Assert.Equal(404, (await Put($"/api/contas-fixas/{Guid.NewGuid()}", "{}")).Codigo);
    }
}
