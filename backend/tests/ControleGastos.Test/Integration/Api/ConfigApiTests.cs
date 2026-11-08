using ControleGastos.Test.Fixtures;

namespace ControleGastos.Test.Integration.Api;

/// <summary>Porte de tests/api/config.test.js.</summary>
public sealed class ConfigApiTests(PostgresFixture db) : ApiTesteBase(db)
{
    private const string Js = "config.test.js";

    private Task<string> Config(string chave) =>
        Db.ConsultarUmAsync<string>("SELECT valor FROM config WHERE chave = @chave", new { chave });

    [Fact]
    [Trait("Vitest", Js + " › OPTIONS retorna 200 e encerra")]
    public async Task Options_Retorna200()
    {
        var r = await Enviar(HttpMethod.Options, "/api/config");
        Assert.Equal(200, r.Codigo);
        Assert.Equal("GET, PUT, OPTIONS", string.Join(", ", r.Mensagem.Headers.GetValues("Access-Control-Allow-Methods")));
    }

    [Fact]
    [Trait("Vitest", Js + " › GET retorna config completa com todos os valores presentes")]
    public async Task Get_Completa()
    {
        await Db.InserirConfigAsync("renda_guilherme", "20000");
        await Db.InserirConfigAsync("renda_esposa", "14000");
        await Db.InserirConfigAsync("meta_fatura", "12000");
        await Db.InserirCategoriaAsync("Pet", 450);
        var cf = await Db.InserirContaFixaAsync("Condomínio", 1600);

        var r = await Get("/api/config");

        Assert.Equal(200, r.Codigo);
        Assert.Equal(
            $$"""{"renda_guilherme":20000,"renda_esposa":14000,"meta_fatura":12000,"total_fixos":1600,"tetos":{"Pet":450},"contas_fixas":[{"id":"{{cf}}","nome":"Condomínio","valor":1600}]}""",
            r.Texto);
    }

    [Fact]
    [Trait("Vitest", Js + " › GET aplica defaults quando config/contas_fixas estão vazias")]
    public async Task Get_Defaults()
    {
        var r = await Get("/api/config");
        Assert.Equal(
            """{"renda_guilherme":0,"renda_esposa":0,"meta_fatura":12000,"total_fixos":0,"tetos":{},"contas_fixas":[]}""",
            r.Texto);
    }

    [Theory]
    [Trait("Vitest", Js + " › GET retorna 500 quando a query de config falha")]
    [Trait("Vitest", Js + " › GET retorna 500 quando a query de categorias falha")]
    [Trait("Vitest", Js + " › GET retorna 500 quando a query de contas_fixas falha")]
    [InlineData("config")]
    [InlineData("categorias")]
    [InlineData("contas_fixas")]
    public async Task Get_QueryFalha_Retorna500(string tabela)
    {
        await using (await Db.TabelaIndisponivelAsync(tabela))
        {
            var r = await Get("/api/config");
            Assert.Equal(500, r.Codigo);
            Assert.Equal($"relation \"{tabela}\" does not exist", r.Erro);
        }
    }

    [Fact]
    [Trait("Vitest", Js + " › PUT atualiza renda_guilherme e renda_esposa")]
    public async Task Put_Ambas()
    {
        await Db.InserirConfigAsync("renda_guilherme", "20000");

        var r = await Put("/api/config", """{"renda_guilherme":21000,"renda_esposa":15000}""");

        Assert.Equal(200, r.Codigo);
        Assert.Equal("""{"updated":true}""", r.Texto);
        Assert.Equal("21000", await Config("renda_guilherme"));
        Assert.Equal("15000", await Config("renda_esposa"));
    }

    [Fact]
    [Trait("Vitest", Js + " › PUT atualiza só renda_guilherme")]
    public async Task Put_SoGuilherme()
    {
        await Db.InserirConfigAsync("renda_esposa", "14000");
        Assert.Equal(200, (await Put("/api/config", """{"renda_guilherme":21000}""")).Codigo);
        Assert.Equal("21000", await Config("renda_guilherme"));
        Assert.Equal("14000", await Config("renda_esposa"));
    }

    [Fact]
    [Trait("Vitest", Js + " › PUT atualiza só renda_esposa")]
    public async Task Put_SoEsposa()
    {
        Assert.Equal(200, (await Put("/api/config", """{"renda_esposa":15000.5}""")).Codigo);
        Assert.Equal("15000.5", await Config("renda_esposa"));
    }

    [Theory]
    [Trait("Vitest", Js + " › PUT rejeita renda_guilherme não numérico")]
    [Trait("Vitest", Js + " › PUT rejeita renda_guilherme negativo")]
    [Trait("Vitest", Js + " › PUT rejeita renda_esposa não numérico")]
    [Trait("Vitest", Js + " › PUT rejeita renda_esposa negativo")]
    [InlineData("""{"renda_guilherme":"abc"}""", "renda_guilherme")]
    [InlineData("""{"renda_guilherme":-5}""", "renda_guilherme")]
    [InlineData("""{"renda_esposa":"abc"}""", "renda_esposa")]
    [InlineData("""{"renda_esposa":-1}""", "renda_esposa")]
    public async Task Put_RendaInvalida_Retorna400(string json, string campo)
    {
        var r = await Put("/api/config", json);
        Assert.Equal(400, r.Codigo);
        Assert.Equal($"{campo} deve ser um número maior ou igual a 0", r.Erro);
    }

    [Fact]
    [Trait("Vitest", Js + " › PUT sem nenhum campo retorna 400")]
    public async Task Put_SemCampos_Retorna400()
    {
        var r = await Put("/api/config", "{}");
        Assert.Equal(400, r.Codigo);
        Assert.Equal("Informe renda_guilherme e/ou renda_esposa", r.Erro);
    }

    [Fact]
    [Trait("Vitest", Js + " › PUT retorna 500 em erro do supabase")]
    public async Task Put_ErroDoBanco_Retorna500()
    {
        await using (await Db.TabelaIndisponivelAsync("config"))
        {
            Assert.Equal(500, (await Put("/api/config", """{"renda_guilherme":1000}""")).Codigo);
        }
    }

    [Fact]
    [Trait("Vitest", Js + " › método não suportado retorna 405")]
    public async Task MetodoNaoSuportado_Retorna405() =>
        Assert.Equal("Method not allowed", (await Delete("/api/config")).Erro);

    // ── total_fixos e tetos ───────────────────────────────────────────────

    [Fact]
    public async Task Get_TotalFixosETetos_ComOsValoresDeProducao()
    {
        // Seed real de supabase-migration-categorias-contas-fixas.sql
        var pasta = Path.Combine(AppContext.BaseDirectory, "Sql");
        var seed = await File.ReadAllTextAsync(Path.Combine(pasta, "supabase-migration-categorias-contas-fixas.sql"), Ct);
        var inicio = seed.IndexOf("INSERT INTO categorias", StringComparison.Ordinal);
        var fim = seed.IndexOf("DELETE FROM config", StringComparison.Ordinal);
        await Db.ExecutarAsync(seed[inicio..fim]);

        var r = await Get("/api/config");

        AssertNumero(14559, r.Corpo.GetProperty("total_fixos"));
        var tetos = r.Corpo.GetProperty("tetos");
        Assert.Equal(14, tetos.EnumerateObject().Count());
        Assert.Equal(12000m, tetos.EnumerateObject().Sum(t => t.Value.GetDecimal())); // SPEC: tetos somam R$ 12.000
        Assert.Equal("Alimentacao fora", tetos.EnumerateObject().First().Name); // ordem por nome
        Assert.Equal(8, r.Corpo.GetProperty("contas_fixas").GetArrayLength());
    }

    [Fact]
    public async Task Get_TotalFixos_ReflereEdicoesDeContasFixas()
    {
        await Db.InserirContaFixaAsync("A", 100.10m);
        var id = await Db.InserirContaFixaAsync("B", 200.20m);
        AssertNumero(300.3m, (await Get("/api/config")).Corpo.GetProperty("total_fixos"));

        await Put($"/api/contas-fixas/{id}", """{"valor":0.05}""");
        AssertNumero(100.15m, (await Get("/api/config")).Corpo.GetProperty("total_fixos"));

        await Delete($"/api/contas-fixas/{id}");
        AssertNumero(100.1m, (await Get("/api/config")).Corpo.GetProperty("total_fixos"));
    }

    [Fact]
    public async Task Get_Tetos_ReflereRenomeacaoECriacaoDeCategorias()
    {
        var id = await Db.InserirCategoriaAsync("Pet", 450);
        await Post("/api/categorias", """{"nome":"Academia","teto":200}""");
        await Put($"/api/categorias/{id}", """{"nome":"Animais","teto":500}""");

        var tetos = (await Get("/api/config")).Corpo.GetProperty("tetos");

        Assert.Equal([("Academia", 200m), ("Animais", 500m)], tetos.EnumerateObject().Select(t => (t.Name, t.Value.GetDecimal())));
    }

    [Fact]
    public async Task Put_Renda_RefleteNoGet()
    {
        await Put("/api/config", """{"renda_guilherme":"21000.50","renda_esposa":0}""");
        var r = await Get("/api/config");
        AssertNumero(21000.5m, r.Corpo.GetProperty("renda_guilherme"));
        AssertNumero(0, r.Corpo.GetProperty("renda_esposa"));
    }
}
