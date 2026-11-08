using ControleGastos.Test.Fixtures;

namespace ControleGastos.Test.Integration.Api;

/// <summary>Porte de tests/api/lancamentos.test.js e tests/api/lancamentos-id.test.js.</summary>
public sealed class LancamentosApiTests(PostgresFixture db) : ApiTesteBase(db)
{
    private const string Js = "lancamentos.test.js";
    private const string JsId = "lancamentos-id.test.js";

    private Task<List<(DateTime Data, string Descricao, decimal Valor, string? Observacao, string Tipo)>> Linhas() =>
        Db.ConsultarAsync<(DateTime, string, decimal, string?, string)>(
            "SELECT data, descricao, valor, observacao, tipo FROM lancamentos ORDER BY data, descricao");

    // ── lancamentos.test.js ───────────────────────────────────────────────

    [Fact]
    [Trait("Vitest", Js + " › OPTIONS retorna 200 e encerra")]
    public async Task Options_Retorna200()
    {
        var r = await Enviar(HttpMethod.Options, "/api/lancamentos");
        Assert.Equal(200, r.Codigo);
        Assert.Empty(r.Texto);
    }

    [Fact]
    [Trait("Vitest", Js + " › GET lista lançamentos do mês convertendo data para BR")]
    public async Task Get_ListaDoMesComDataBr()
    {
        await Db.InserirLancamentoAsync("2026-06-05", "Pet", 10);
        await Db.InserirLancamentoAsync("2026-07-05", "Pet", 20);

        var r = await Get("/api/lancamentos?mes=2026-06");

        Assert.Equal(200, r.Codigo);
        var item = Assert.Single(r.Corpo.EnumerateArray());
        Assert.Equal("05/06/2026", item.GetProperty("data").GetString());
        AssertNumero(10, item.GetProperty("valor"));
    }

    [Fact]
    [Trait("Vitest", Js + " › GET lista todos os lançamentos quando mes não é informado")]
    public async Task Get_SemMes_ListaTodos()
    {
        var vazio = await Get("/api/lancamentos");
        Assert.Equal("[]", vazio.Texto);

        await Db.InserirLancamentoAsync("2026-06-05", "Pet", 10);
        await Db.InserirLancamentoAsync("2027-01-05", "Pet", 20);
        Assert.Equal(2, (await Get("/api/lancamentos")).Corpo.GetArrayLength());
    }

    [Fact]
    [Trait("Vitest", Js + " › GET retorna 500 em erro do supabase")]
    public async Task Get_ErroDoBanco_Retorna500()
    {
        await using (await Db.TabelaIndisponivelAsync("lancamentos"))
        {
            var r = await Get("/api/lancamentos");
            Assert.Equal(500, r.Codigo);
            Assert.Equal("relation \"lancamentos\" does not exist", r.Erro);
        }
    }

    // "GET mantém data como null quando a linha não tem data": no banco real data é NOT NULL;
    // coberto em MappingsTests.Lancamento_SemData_MantemNull e DataBrJsonConverterTests.

    [Fact]
    [Trait("Vitest", Js + " › POST rejeita quando faltam campos obrigatórios")]
    public async Task Post_SemObrigatorios_Retorna400()
    {
        var r = await Post("/api/lancamentos", "{}");
        Assert.Equal(400, r.Codigo);
        Assert.Equal("data, categoria e valor são obrigatórios", r.Erro);
    }

    [Fact]
    [Trait("Vitest", Js + " › POST cria lançamento à vista (1 parcela)")]
    public async Task Post_AVista()
    {
        var r = await Post("/api/lancamentos",
            """{"data":"01/06/2026","descricao":"Almoço","categoria":"Pet","valor":100,"observacao":"obs","tipo":"A vista"}""");

        Assert.Equal(201, r.Codigo);
        Assert.Equal(1, r.Corpo.GetProperty("inserted").GetInt32());
        var id = Assert.Single(r.Corpo.GetProperty("ids").EnumerateArray()).GetGuid();
        Assert.Equal(id, await Db.ConsultarUmAsync<Guid>("SELECT id FROM lancamentos"));
        Assert.Equal((new DateTime(2026, 6, 1), "Almoço", 100m, (string?)"obs", "A vista"), Assert.Single(await Linhas()));
    }

    [Fact]
    [Trait("Vitest", Js + " › POST cria lançamento parcelado, dividindo o valor e numerando a descrição")]
    public async Task Post_Parcelado()
    {
        var r = await Post("/api/lancamentos",
            """{"data":"01/06/2026","descricao":"Viagem","categoria":"Viagem / Lazer","valor":300,"parcelas":3,"tipo":"Parcelado"}""");

        Assert.Equal(201, r.Codigo);
        Assert.Equal(3, r.Corpo.GetProperty("inserted").GetInt32());
        Assert.Equal(3, r.Corpo.GetProperty("ids").GetArrayLength());
        var linhas = await Linhas();
        Assert.Equal(["Viagem (1/3)", "Viagem (2/3)", "Viagem (3/3)"], linhas.Select(l => l.Descricao));
        Assert.Equal([new DateTime(2026, 6, 1), new DateTime(2026, 7, 1), new DateTime(2026, 8, 1)], linhas.Select(l => l.Data));
        Assert.All(linhas, l => Assert.Equal(100m, l.Valor));
    }

    [Fact]
    [Trait("Vitest", Js + " › POST cria lançamento parcelado sem descrição, numerando apenas o sufixo")]
    public async Task Post_ParceladoSemDescricao()
    {
        var r = await Post("/api/lancamentos", """{"data":"01/06/2026","categoria":"Pet","valor":100,"parcelas":2}""");

        Assert.Equal(2, r.Corpo.GetProperty("inserted").GetInt32());
        Assert.Equal(["(1/2)", "(2/2)"], (await Linhas()).Select(l => l.Descricao));
    }

    [Fact]
    [Trait("Vitest", Js + " › POST usa descrição e observação vazias por padrão quando não informadas")]
    public async Task Post_Padroes()
    {
        var r = await Post("/api/lancamentos", """{"data":"01/06/2026","categoria":"Pet","valor":50}""");

        Assert.Equal(201, r.Codigo);
        Assert.Equal((new DateTime(2026, 6, 1), "", 50m, (string?)null, "A vista"), Assert.Single(await Linhas()));
    }

    [Fact]
    [Trait("Vitest", Js + " › POST retorna 500 em erro do supabase")]
    public async Task Post_ErroDoBanco_Retorna500()
    {
        var r = await Post("/api/lancamentos", """{"data":"01/06/2026","categoria":"Pet","valor":50,"tipo":"Outro"}""");

        Assert.Equal(500, r.Codigo);
        Assert.Contains("lancamentos_tipo_check", r.Erro, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Vitest", Js + " › método não suportado retorna 405")]
    public async Task MetodoNaoSuportado_Retorna405()
    {
        var r = await Delete("/api/lancamentos");
        Assert.Equal(405, r.Codigo);
        Assert.Equal("Method not allowed", r.Erro);
    }

    // ── lancamentos-id.test.js ────────────────────────────────────────────

    [Fact]
    [Trait("Vitest", JsId + " › OPTIONS retorna 200 e encerra")]
    public async Task Id_Options_Retorna200()
    {
        var r = await Enviar(HttpMethod.Options, "/api/lancamentos/1");
        Assert.Equal(200, r.Codigo);
        Assert.Equal("PUT, DELETE, OPTIONS", string.Join(", ", r.Mensagem.Headers.GetValues("Access-Control-Allow-Methods")));
    }

    [Fact]
    [Trait("Vitest", JsId + " › sem id retorna 400")]
    public async Task Id_SemId_NaoChegaNaRotaDeId()
    {
        // No ASP.NET "/api/lancamentos/" sem id cai na rota da coleção, que não aceita DELETE → 405 { error }.
        var r = await Delete("/api/lancamentos/");
        Assert.Equal(405, r.Codigo);
        Assert.Equal("Method not allowed", r.Erro);
    }

    [Fact]
    [Trait("Vitest", JsId + " › DELETE exclui lançamento")]
    public async Task Id_Delete_Exclui()
    {
        var id = await Db.InserirLancamentoAsync("2026-06-01", "Pet", 1);

        var r = await Delete($"/api/lancamentos/{id}");

        Assert.Equal(200, r.Codigo);
        Assert.Equal("""{"deleted":true}""", r.Texto);
        Assert.Equal(0L, await Db.ConsultarUmAsync<long>("SELECT COUNT(*) FROM lancamentos"));
    }

    [Fact]
    [Trait("Vitest", JsId + " › DELETE retorna 500 em erro do supabase")]
    public async Task Id_Delete_ErroDoBanco_Retorna500()
    {
        var r = await Delete("/api/lancamentos/nao-e-uuid");
        Assert.Equal(500, r.Codigo);
        Assert.Equal("invalid input syntax for type uuid: \"nao-e-uuid\"", r.Erro);
    }

    [Fact]
    [Trait("Vitest", JsId + " › PUT atualiza todos os campos, convertendo data BR -> ISO e de volta")]
    public async Task Id_Put_TodosOsCampos()
    {
        var id = await Db.InserirLancamentoAsync("2026-01-01", "Guitarra", 1);

        var r = await Put($"/api/lancamentos/{id}",
            """{"data":"05/06/2026","descricao":"Nova","categoria":"Pet","valor":99,"observacao":"obs","tipo":"A vista"}""");

        Assert.Equal(200, r.Codigo);
        Assert.True(r.Corpo.GetProperty("updated").GetBoolean());
        var l = r.Corpo.GetProperty("lancamento");
        Assert.Equal("05/06/2026", l.GetProperty("data").GetString());
        Assert.Equal(("Nova", "Pet", "obs", "A vista"),
            (l.GetProperty("descricao").GetString(), l.GetProperty("categoria").GetString(),
             l.GetProperty("observacao").GetString(), l.GetProperty("tipo").GetString()));
        AssertNumero(99, l.GetProperty("valor"));
        Assert.Equal(new DateTime(2026, 6, 5), await Db.ConsultarUmAsync<DateTime>("SELECT data FROM lancamentos"));
    }

    [Fact]
    [Trait("Vitest", JsId + " › PUT aceita data já em formato ISO (contém \"-\")")]
    public async Task Id_Put_DataIso()
    {
        var id = await Db.InserirLancamentoAsync("2026-01-01", "Pet", 10);

        var r = await Put($"/api/lancamentos/{id}", """{"data":"2026-06-05"}""");

        Assert.Equal(200, r.Codigo);
        Assert.Equal("05/06/2026", r.Corpo.GetProperty("lancamento").GetProperty("data").GetString());
    }

    [Fact]
    [Trait("Vitest", JsId + " › PUT atualiza apenas campos parciais (sem data)")]
    public async Task Id_Put_Parcial()
    {
        var id = await Db.InserirLancamentoAsync("2026-06-05", "Pet", 1, "manter");

        var r = await Put($"/api/lancamentos/{id}", """{"valor":10}""");

        Assert.Equal(200, r.Codigo);
        var l = r.Corpo.GetProperty("lancamento");
        AssertNumero(10, l.GetProperty("valor"));
        Assert.Equal("manter", l.GetProperty("descricao").GetString());
        Assert.Equal("05/06/2026", l.GetProperty("data").GetString());
    }

    [Fact]
    [Trait("Vitest", JsId + " › PUT retorna 500 em erro do supabase")]
    public async Task Id_Put_ErroDoBanco_Retorna500()
    {
        var r = await Put("/api/lancamentos/nao-e-uuid", """{"valor":10}""");
        Assert.Equal(500, r.Codigo);
        Assert.Equal("invalid input syntax for type uuid: \"nao-e-uuid\"", r.Erro);
    }

    // "PUT mantém data como null quando a linha atualizada não tem data": idem GET (data NOT NULL no banco).

    [Fact]
    [Trait("Vitest", JsId + " › método não suportado retorna 405")]
    public async Task Id_MetodoNaoSuportado_Retorna405()
    {
        var r = await Enviar(HttpMethod.Patch, "/api/lancamentos/1", "{}");
        Assert.Equal(405, r.Codigo);
        Assert.Equal("Method not allowed", r.Erro);
    }

    // ── Cenários novos (desvios aprovados D1–D7 e créditos) ───────────────

    [Fact]
    public async Task Post_CreditoNegativo_Grava()
    {
        var r = await Post("/api/lancamentos", """{"data":"10/06/2026","descricao":"Estorno","categoria":"Pet","valor":-75.5}""");

        Assert.Equal(201, r.Codigo);
        Assert.Equal(-75.5m, Assert.Single(await Linhas()).Valor);
        AssertNumero(-75.5m, (await Get("/api/lancamentos?mes=2026-06")).Corpo[0].GetProperty("valor"));
    }

    [Fact]
    public async Task Post_Parcela31DeJaneiro_SegundaCaiEm28DeFevereiro()
    {
        await Post("/api/lancamentos", """{"data":"31/01/2026","categoria":"Pet","valor":100,"parcelas":2}""");
        Assert.Equal([new DateTime(2026, 1, 31), new DateTime(2026, 2, 28)], (await Linhas()).Select(l => l.Data));
    }

    [Theory]
    [InlineData("""{"data":"01/06/2026","categoria":"Pet","valor":"abc"}""", "valor deve ser um número")]
    [InlineData("""{"data":"01/06/2026","categoria":"Pet","valor":"12abc"}""", "valor deve ser um número")]
    [InlineData("""{"data":"31/02/2026","categoria":"Pet","valor":10}""", "data deve estar no formato DD/MM/YYYY")]
    [InlineData("""{"data":"x","categoria":"Pet","valor":10}""", "data deve estar no formato DD/MM/YYYY")]
    [InlineData("""{"data":"01/06/2026","categoria":"Pet","valor":0}""", "data, categoria e valor são obrigatórios")]
    [InlineData("""{"data":"01/06/2026","categoria":"","valor":10}""", "data, categoria e valor são obrigatórios")]
    public async Task Post_EntradasInvalidas_Retorna400(string json, string mensagem)
    {
        var r = await Post("/api/lancamentos", json);
        Assert.Equal(400, r.Codigo);
        Assert.Equal(mensagem, r.Erro);
    }

    [Fact]
    public async Task Post_ValorComoString_Aceita()
    {
        Assert.Equal(201, (await Post("/api/lancamentos", """{"data":"01/06/2026","categoria":"Pet","valor":"19.90"}""")).Codigo);
        Assert.Equal(19.9m, Assert.Single(await Linhas()).Valor);
    }

    [Fact]
    public async Task Post_ParcelasNegativas_InsereUma()
    {
        var r = await Post("/api/lancamentos", """{"data":"01/06/2026","categoria":"Pet","valor":10,"parcelas":-2}""");
        Assert.Equal(1, r.Corpo.GetProperty("inserted").GetInt32());
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("2026-13")]
    public async Task Get_MesInvalido_Retorna400(string mes)
    {
        var r = await Get($"/api/lancamentos?mes={mes}");
        Assert.Equal(400, r.Codigo);
        Assert.Equal("mes deve estar no formato YYYY-MM", r.Erro);
    }

    [Fact]
    public async Task Put_Inexistente_Retorna404()
    {
        var r = await Put($"/api/lancamentos/{Guid.NewGuid()}", """{"valor":10}""");
        Assert.Equal(404, r.Codigo);
        Assert.Equal("Lançamento não encontrado", r.Erro);
    }

    [Fact]
    public async Task Put_SemCampos_DevolveAtual()
    {
        var id = await Db.InserirLancamentoAsync("2026-06-05", "Pet", 7);
        var r = await Put($"/api/lancamentos/{id}", "{}");
        Assert.Equal(200, r.Codigo);
        AssertNumero(7, r.Corpo.GetProperty("lancamento").GetProperty("valor"));
    }

    [Fact]
    public async Task Put_ValorNegativoENullsEmTextos()
    {
        var id = await Db.InserirLancamentoAsync("2026-06-05", "Pet", 7, "d", "o");

        var r = await Put($"/api/lancamentos/{id}", """{"valor":-7,"descricao":null,"observacao":""}""");

        var l = r.Corpo.GetProperty("lancamento");
        AssertNumero(-7, l.GetProperty("valor"));
        Assert.Equal(System.Text.Json.JsonValueKind.Null, l.GetProperty("descricao").ValueKind);
        Assert.Equal("", l.GetProperty("observacao").GetString());
    }

    [Fact]
    public async Task Delete_Inexistente_Retorna200()
    {
        var r = await Delete($"/api/lancamentos/{Guid.NewGuid()}");
        Assert.Equal(200, r.Codigo);
        Assert.Equal("""{"deleted":true}""", r.Texto);
    }
}
