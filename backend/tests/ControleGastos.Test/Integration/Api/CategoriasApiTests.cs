using ControleGastos.Test.Fixtures;

namespace ControleGastos.Test.Integration.Api;

/// <summary>Porte de tests/api/categorias.test.js e tests/api/categorias-id.test.js.</summary>
public sealed class CategoriasApiTests(PostgresFixture db) : ApiTesteBase(db)
{
    private const string Js = "categorias.test.js";
    private const string JsId = "categorias-id.test.js";

    private Task<long> LancamentosDa(string categoria) =>
        Db.ConsultarUmAsync<long>("SELECT COUNT(*) FROM lancamentos WHERE categoria = @categoria", new { categoria });

    // ── categorias.test.js ────────────────────────────────────────────────

    [Fact]
    [Trait("Vitest", Js + " › OPTIONS retorna 200 e encerra")]
    public async Task Options_Retorna200() => Assert.Equal(200, (await Enviar(HttpMethod.Options, "/api/categorias")).Codigo);

    [Fact]
    [Trait("Vitest", Js + " › GET lista categorias ordenadas por nome, com teto numérico")]
    public async Task Get_OrdenadasComTetoNumerico()
    {
        await Db.InserirCategoriaAsync("Pet", 450);
        await Db.InserirCategoriaAsync("Alimentacao fora", 2000);

        var r = await Get("/api/categorias");

        Assert.Equal(200, r.Codigo);
        Assert.Equal(["Alimentacao fora", "Pet"], r.Corpo.EnumerateArray().Select(c => c.GetProperty("nome").GetString()));
        var pet = r.Corpo[1];
        AssertNumero(450, pet.GetProperty("teto"));
        Assert.Equal(["id", "nome", "teto", "created_at"], pet.EnumerateObject().Select(p => p.Name));
    }

    [Fact]
    [Trait("Vitest", Js + " › GET retorna 500 em erro do supabase")]
    public async Task Get_ErroDoBanco_Retorna500()
    {
        await using (await Db.TabelaIndisponivelAsync("categorias"))
        {
            var r = await Get("/api/categorias");
            Assert.Equal(500, r.Codigo);
            Assert.Equal("relation \"categorias\" does not exist", r.Erro);
        }
    }

    [Fact]
    [Trait("Vitest", Js + " › POST cria categoria válida")]
    public async Task Post_CriaComTrim()
    {
        var r = await Post("/api/categorias", """{"nome":"  Nova Cat  ","teto":100}""");

        Assert.Equal(201, r.Codigo);
        Assert.Equal("Nova Cat", r.Corpo.GetProperty("nome").GetString());
        AssertNumero(100, r.Corpo.GetProperty("teto"));
        Assert.Equal(r.Corpo.GetProperty("id").GetGuid(), await Db.ConsultarUmAsync<Guid>("SELECT id FROM categorias"));
    }

    [Fact]
    [Trait("Vitest", Js + " › POST com teto omitido usa 0")]
    public async Task Post_TetoOmitido_UsaZero()
    {
        var r = await Post("/api/categorias", """{"nome":"Sem Teto"}""");
        Assert.Equal(201, r.Codigo);
        AssertNumero(0, r.Corpo.GetProperty("teto"));
    }

    [Theory]
    [Trait("Vitest", Js + " › POST rejeita nome vazio (string em branco)")]
    [Trait("Vitest", Js + " › POST rejeita nome ausente no body")]
    [InlineData("""{"nome":"   ","teto":10}""")]
    [InlineData("""{"teto":10}""")]
    public async Task Post_NomeVazioOuAusente_Retorna400(string json)
    {
        var r = await Post("/api/categorias", json);
        Assert.Equal(400, r.Codigo);
        Assert.Equal("nome é obrigatório", r.Erro);
    }

    [Theory]
    [Trait("Vitest", Js + " › POST rejeita teto inválido (NaN)")]
    [Trait("Vitest", Js + " › POST rejeita teto negativo")]
    [InlineData("""{"nome":"X","teto":"abc"}""")]
    [InlineData("""{"nome":"X","teto":-1}""")]
    public async Task Post_TetoInvalidoOuNegativo_Retorna400(string json)
    {
        var r = await Post("/api/categorias", json);
        Assert.Equal(400, r.Codigo);
        Assert.Equal("teto deve ser um número maior ou igual a 0", r.Erro);
    }

    [Fact]
    [Trait("Vitest", Js + " › POST retorna 409 quando nome já existe (unique violation)")]
    public async Task Post_NomeDuplicado_Retorna409()
    {
        await Db.InserirCategoriaAsync("Repetida", 1);
        var r = await Post("/api/categorias", """{"nome":"Repetida","teto":10}""");
        Assert.Equal(409, r.Codigo);
        Assert.Equal("Já existe uma categoria com esse nome", r.Erro);
    }

    [Fact]
    [Trait("Vitest", Js + " › POST retorna 500 em outro erro do supabase")]
    public async Task Post_OutroErro_Retorna500()
    {
        var r = await Post("/api/categorias", """{"nome":"X","teto":100000000000}""");
        Assert.Equal(500, r.Codigo);
        Assert.Equal("numeric field overflow", r.Erro);
    }

    [Fact]
    [Trait("Vitest", Js + " › método não suportado retorna 405")]
    public async Task MetodoNaoSuportado_Retorna405() =>
        Assert.Equal("Method not allowed", (await Enviar(HttpMethod.Patch, "/api/categorias", "{}")).Erro);

    // ── categorias-id.test.js ─────────────────────────────────────────────

    [Fact]
    [Trait("Vitest", JsId + " › OPTIONS retorna 200 e encerra")]
    public async Task Id_Options_Retorna200() => Assert.Equal(200, (await Enviar(HttpMethod.Options, "/api/categorias/1")).Codigo);

    [Fact]
    [Trait("Vitest", JsId + " › sem id retorna 400")]
    public async Task Id_SemId_NaoChegaNaRotaDeId() =>
        Assert.Equal(405, (await Put("/api/categorias/", "{}")).Codigo); // rota da coleção não aceita PUT

    [Fact]
    [Trait("Vitest", JsId + " › PUT atualiza teto sem mudar nome (não roda job de rename)")]
    public async Task Id_Put_SoTeto()
    {
        var id = await Db.InserirCategoriaAsync("Pet", 450);
        await Db.InserirLancamentoAsync("2026-06-01", "Pet", 1);

        var r = await Put($"/api/categorias/{id}", """{"teto":500}""");

        Assert.Equal(200, r.Codigo);
        Assert.True(r.Corpo.GetProperty("updated").GetBoolean());
        Assert.Equal("Pet", r.Corpo.GetProperty("categoria").GetProperty("nome").GetString());
        AssertNumero(500, r.Corpo.GetProperty("categoria").GetProperty("teto"));
        Assert.Equal(0, r.Corpo.GetProperty("lancamentos_atualizados").GetInt32());
    }

    [Fact]
    [Trait("Vitest", JsId + " › PUT renomeia categoria e propaga para lançamentos históricos (job)")]
    public async Task Id_Put_RenomeiaEPropaga()
    {
        var id = await Db.InserirCategoriaAsync("Pet", 450);
        await Db.InserirLancamentoAsync("2026-06-01", "Pet", 1);
        await Db.InserirLancamentoAsync("2027-03-01", "Pet", 2);
        await Db.InserirLancamentoAsync("2026-06-01", "Guitarra", 3);

        var r = await Put($"/api/categorias/{id}", """{"nome":"Animais"}""");

        Assert.Equal(200, r.Codigo);
        Assert.Equal("Animais", r.Corpo.GetProperty("categoria").GetProperty("nome").GetString());
        AssertNumero(450, r.Corpo.GetProperty("categoria").GetProperty("teto"));
        Assert.Equal(2, r.Corpo.GetProperty("lancamentos_atualizados").GetInt32());
        Assert.Equal(2L, await LancamentosDa("Animais"));
        Assert.Equal(0L, await LancamentosDa("Pet"));
        Assert.Equal(1L, await LancamentosDa("Guitarra"));
    }

    [Fact]
    [Trait("Vitest", JsId + " › PUT não roda job quando o nome enviado é igual ao atual")]
    public async Task Id_Put_MesmoNome_NaoRodaJob()
    {
        var id = await Db.InserirCategoriaAsync("Pet", 450);
        await Db.InserirLancamentoAsync("2026-06-01", "Pet", 1);

        var r = await Put($"/api/categorias/{id}", """{"nome":"Pet"}""");

        Assert.Equal(0, r.Corpo.GetProperty("lancamentos_atualizados").GetInt32());
    }

    [Fact]
    [Trait("Vitest", JsId + " › PUT retorna 404 quando categoria não existe")]
    public async Task Id_Put_Inexistente_Retorna404()
    {
        var r = await Put($"/api/categorias/{Guid.NewGuid()}", """{"teto":10}""");
        Assert.Equal(404, r.Codigo);
        Assert.Equal("Categoria não encontrada", r.Erro);
    }

    [Fact]
    [Trait("Vitest", JsId + " › PUT retorna 500 em erro inesperado ao buscar categoria atual")]
    public async Task Id_Put_ErroAoBuscar_Retorna500()
    {
        await using (await Db.TabelaIndisponivelAsync("categorias"))
        {
            var r = await Put($"/api/categorias/{Guid.NewGuid()}", """{"teto":10}""");
            Assert.Equal(500, r.Codigo);
            Assert.Equal("relation \"categorias\" does not exist", r.Erro);
        }
    }

    [Theory]
    [Trait("Vitest", JsId + " › PUT rejeita nome vazio (string em branco)")]
    [Trait("Vitest", JsId + " › PUT rejeita nome enviado como string vazia")]
    [InlineData("""{"nome":"   "}""")]
    [InlineData("""{"nome":""}""")]
    public async Task Id_Put_NomeVazio_Retorna400(string json)
    {
        var id = await Db.InserirCategoriaAsync("Pet", 450);
        var r = await Put($"/api/categorias/{id}", json);
        Assert.Equal(400, r.Codigo);
        Assert.Equal("nome não pode ser vazio", r.Erro);
    }

    [Theory]
    [Trait("Vitest", JsId + " › PUT rejeita teto inválido")]
    [Trait("Vitest", JsId + " › PUT rejeita teto negativo")]
    [InlineData("""{"teto":"abc"}""")]
    [InlineData("""{"teto":-1}""")]
    public async Task Id_Put_TetoInvalido_Retorna400(string json)
    {
        var id = await Db.InserirCategoriaAsync("Pet", 450);
        var r = await Put($"/api/categorias/{id}", json);
        Assert.Equal(400, r.Codigo);
        Assert.Equal("teto deve ser um número maior ou igual a 0", r.Erro);
    }

    [Fact]
    [Trait("Vitest", JsId + " › PUT retorna 409 quando o novo nome já existe (unique violation)")]
    public async Task Id_Put_NomeDuplicado_Retorna409()
    {
        var id = await Db.InserirCategoriaAsync("Pet", 450);
        await Db.InserirCategoriaAsync("Guitarra", 300);

        var r = await Put($"/api/categorias/{id}", """{"nome":"Guitarra"}""");

        Assert.Equal(409, r.Codigo);
        Assert.Equal("Já existe uma categoria com esse nome", r.Erro);
    }

    [Fact]
    [Trait("Vitest", JsId + " › PUT retorna 500 em outro erro ao atualizar")]
    public async Task Id_Put_OutroErro_Retorna500()
    {
        var id = await Db.InserirCategoriaAsync("Pet", 450);
        var r = await Put($"/api/categorias/{id}", """{"teto":100000000000}""");
        Assert.Equal(500, r.Codigo);
        Assert.Equal("numeric field overflow", r.Erro);
    }

    [Fact]
    [Trait("Vitest", JsId + " › PUT retorna 500 quando o job de rename falha")]
    public async Task Id_Put_JobFalha_Retorna500EDesfaz()
    {
        var id = await Db.InserirCategoriaAsync("Pet", 450);
        await Db.InserirLancamentoAsync("2026-06-01", "Pet", 1);

        await using (await Db.FalharEmAsync("lancamentos", "UPDATE"))
        {
            var r = await Put($"/api/categorias/{id}", """{"nome":"Animais"}""");
            Assert.Equal(500, r.Codigo);
            Assert.Equal("falha simulada em lancamentos", r.Erro);
        }

        // Melhoria aprovada: transação — a categoria não fica renomeada com lançamentos órfãos.
        Assert.Equal("Pet", await Db.ConsultarUmAsync<string>("SELECT nome FROM categorias WHERE id = @id", new { id }));
        Assert.Equal(1L, await LancamentosDa("Pet"));
    }

    [Fact]
    [Trait("Vitest", JsId + " › DELETE exclui categoria sem lançamentos associados")]
    public async Task Id_Delete_Exclui()
    {
        var id = await Db.InserirCategoriaAsync("Pet", 450);

        var r = await Delete($"/api/categorias/{id}");

        Assert.Equal(200, r.Codigo);
        Assert.Equal("""{"deleted":true}""", r.Texto);
        Assert.Equal(0L, await Db.ConsultarUmAsync<long>("SELECT COUNT(*) FROM categorias"));
    }

    [Fact]
    [Trait("Vitest", JsId + " › DELETE bloqueia exclusão quando há lançamentos em uso")]
    public async Task Id_Delete_EmUso_Retorna409()
    {
        var id = await Db.InserirCategoriaAsync("Pet", 450);
        for (var i = 0; i < 3; i++)
        {
            await Db.InserirLancamentoAsync("2026-06-01", "Pet", 1);
        }

        var r = await Delete($"/api/categorias/{id}");

        Assert.Equal(409, r.Codigo);
        Assert.Equal("Não é possível excluir: 3 lançamento(s) usam essa categoria", r.Erro);
        Assert.Equal(3, r.Corpo.GetProperty("lancamentos_em_uso").GetInt32());
        Assert.Equal(1L, await Db.ConsultarUmAsync<long>("SELECT COUNT(*) FROM categorias"));
    }

    [Fact]
    [Trait("Vitest", JsId + " › DELETE retorna 404 quando categoria não existe")]
    public async Task Id_Delete_Inexistente_Retorna404() =>
        Assert.Equal(404, (await Delete($"/api/categorias/{Guid.NewGuid()}")).Codigo);

    [Fact]
    [Trait("Vitest", JsId + " › DELETE retorna 500 em erro inesperado ao buscar categoria")]
    public async Task Id_Delete_ErroAoBuscar_Retorna500()
    {
        var r = await Delete("/api/categorias/nao-e-uuid");
        Assert.Equal(500, r.Codigo);
        Assert.Equal("invalid input syntax for type uuid: \"nao-e-uuid\"", r.Erro);
    }

    [Fact]
    [Trait("Vitest", JsId + " › DELETE retorna 500 quando a contagem de lançamentos falha")]
    public async Task Id_Delete_ContagemFalha_Retorna500()
    {
        var id = await Db.InserirCategoriaAsync("Pet", 450);
        await using (await Db.TabelaIndisponivelAsync("lancamentos"))
        {
            var r = await Delete($"/api/categorias/{id}");
            Assert.Equal(500, r.Codigo);
            Assert.Equal("relation \"lancamentos\" does not exist", r.Erro);
        }
    }

    [Fact]
    [Trait("Vitest", JsId + " › DELETE retorna 500 quando o delete falha")]
    public async Task Id_Delete_DeleteFalha_Retorna500()
    {
        var id = await Db.InserirCategoriaAsync("Pet", 450);
        await using (await Db.FalharEmAsync("categorias", "DELETE"))
        {
            var r = await Delete($"/api/categorias/{id}");
            Assert.Equal(500, r.Codigo);
            Assert.Equal("falha simulada em categorias", r.Erro);
        }
    }

    [Fact]
    [Trait("Vitest", JsId + " › método não suportado retorna 405")]
    public async Task Id_MetodoNaoSuportado_Retorna405() =>
        Assert.Equal(405, (await Enviar(HttpMethod.Patch, "/api/categorias/1", "{}")).Codigo);

    // ── Cenários adicionais ───────────────────────────────────────────────

    [Fact]
    public async Task Id_Put_InexistenteComBodyInvalido_Retorna404AntesDeValidar() =>
        Assert.Equal(404, (await Put($"/api/categorias/{Guid.NewGuid()}", """{"teto":-1}""")).Codigo);

    [Fact]
    public async Task Id_Put_NomeETetoJuntos()
    {
        var id = await Db.InserirCategoriaAsync("Pet", 450);
        await Db.InserirLancamentoAsync("2026-06-01", "Pet", 1);

        var r = await Put($"/api/categorias/{id}", """{"nome":"Animais","teto":"600"}""");

        AssertNumero(600, r.Corpo.GetProperty("categoria").GetProperty("teto"));
        Assert.Equal(1, r.Corpo.GetProperty("lancamentos_atualizados").GetInt32());
    }

    [Fact]
    public async Task Id_Delete_DepoisDeRenomear_ContaPeloNomeNovo()
    {
        var id = await Db.InserirCategoriaAsync("Pet", 450);
        await Db.InserirLancamentoAsync("2026-06-01", "Pet", 1);
        await Put($"/api/categorias/{id}", """{"nome":"Animais"}""");

        var r = await Delete($"/api/categorias/{id}");

        Assert.Equal(409, r.Codigo);
        Assert.Equal(1, r.Corpo.GetProperty("lancamentos_em_uso").GetInt32());
    }
}
