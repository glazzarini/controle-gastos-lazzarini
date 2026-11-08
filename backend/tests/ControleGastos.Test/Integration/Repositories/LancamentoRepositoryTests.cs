using ControleGastos.Core.Entities;
using ControleGastos.Core.Repositories;
using ControleGastos.Test.Fixtures;
using Npgsql;

namespace ControleGastos.Test.Integration.Repositories;

public sealed class LancamentoRepositoryTests(PostgresFixture db) : IntegracaoBase(db)
{
    private LancamentoRepository Repo => new(new NpgsqlConnectionFactory(Db.ConnectionString));

    [Fact]
    public async Task Inserir_GravaTodasAsLinhasERetornaIds()
    {
        var ids = await Repo.InserirAsync([
            new NovoLancamento(new DateTime(2026, 6, 1), "TV (1/2)", "Pet", 50, "obs", "Parcelado"),
            new NovoLancamento(new DateTime(2026, 7, 1), "TV (2/2)", "Pet", -50, null, "A vista"),
        ], Ct);

        Assert.Equal(2, ids.Count);
        var linhas = await Db.ConsultarAsync<(DateTime Data, string Descricao, decimal Valor, string? Observacao, string Tipo)>(
            "SELECT data, descricao, valor, observacao, tipo FROM lancamentos ORDER BY data");
        Assert.Equal((new DateTime(2026, 6, 1), "TV (1/2)", 50m, (string?)"obs", "Parcelado"), linhas[0]);
        Assert.Equal((new DateTime(2026, 7, 1), "TV (2/2)", -50m, (string?)null, "A vista"), linhas[1]);
    }

    [Fact]
    public async Task Inserir_FalhaNumaParcela_DesfazTudo()
    {
        var erro = await Assert.ThrowsAsync<PostgresException>(() => Repo.InserirAsync([
            new NovoLancamento(new DateTime(2026, 6, 1), "ok", "Pet", 10, null, "A vista"),
            new NovoLancamento(new DateTime(2026, 7, 1), "tipo inválido", "Pet", 10, null, "Invalido"),
        ], Ct));

        Assert.Equal(PostgresErrorCodes.CheckViolation, erro.SqlState);
        Assert.Equal(0L, await Db.ConsultarUmAsync<long>("SELECT COUNT(*) FROM lancamentos"));
    }

    [Fact]
    public async Task Listar_Todos_OrdenaPorDataEDepoisCreatedAtDesc()
    {
        var antigo = await Db.InserirLancamentoAsync("2026-06-01", "Pet", 1, createdAt: new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc));
        var mesmoDiaNovo = await Db.InserirLancamentoAsync("2026-06-01", "Pet", 2, createdAt: new DateTime(2026, 6, 1, 11, 0, 0, DateTimeKind.Utc));
        var outroMes = await Db.InserirLancamentoAsync("2026-07-15", "Pet", 3);

        var lista = await Repo.ListarAsync(null, null, Ct);

        Assert.Equal([outroMes, mesmoDiaNovo, antigo], lista.Select(l => l.Id));
    }

    [Fact]
    public async Task Listar_Periodo_IncluiPrimeiroEUltimoDia()
    {
        await Db.InserirLancamentoAsync("2026-05-31", "Pet", 1);
        var primeiro = await Db.InserirLancamentoAsync("2026-06-01", "Pet", 2);
        var ultimo = await Db.InserirLancamentoAsync("2026-06-30", "Pet", 3);
        await Db.InserirLancamentoAsync("2026-07-01", "Pet", 4);

        var lista = await Repo.ListarAsync(new DateTime(2026, 6, 1), new DateTime(2026, 6, 30), Ct);

        Assert.Equal([ultimo, primeiro], lista.Select(l => l.Id));
    }

    [Fact]
    public async Task Listar_SoInicioInformado_ListaTodos()
    {
        await Db.InserirLancamentoAsync("2026-05-31", "Pet", 1);
        Assert.Single(await Repo.ListarAsync(new DateTime(2026, 6, 1), null, Ct));
    }

    [Fact]
    public async Task Listar_MapeiaTodasAsColunas()
    {
        var id = await Db.InserirLancamentoAsync("2026-06-05", "Pet", 99.9m, "desc", "obs", "Parcelado");

        var l = Assert.Single(await Repo.ListarAsync(null, null, Ct));

        Assert.Equal(id, l.Id);
        Assert.Equal(new DateTime(2026, 6, 5), l.Data);
        Assert.Equal(("desc", "Pet", 99.9m, "obs", "Parcelado"), (l.Descricao, l.Categoria, l.Valor, l.Observacao, l.Tipo));
        Assert.NotNull(l.CreatedAt);
    }

    [Fact]
    public async Task ObterPorId_ExistenteEInexistente()
    {
        var id = await Db.InserirLancamentoAsync("2026-06-05", "Pet", 1);

        Assert.Equal(id, (await Repo.ObterPorIdAsync(id.ToString(), Ct))!.Id);
        Assert.Null(await Repo.ObterPorIdAsync(Guid.NewGuid().ToString(), Ct));
    }

    [Fact]
    public async Task IdNaoUuid_ErroDoPostgres()
    {
        var erro = await Assert.ThrowsAsync<PostgresException>(() => Repo.ObterPorIdAsync("x", Ct));
        Assert.Equal("invalid input syntax for type uuid: \"x\"", erro.MessageText);
    }

    [Fact]
    public async Task Atualizar_TodosOsCampos()
    {
        var id = await Db.InserirLancamentoAsync("2026-06-05", "Pet", 1, "a", "b");

        var l = await Repo.AtualizarAsync(id.ToString(), new LancamentoAlteracoes
        {
            AlterarData = true, Data = new DateTime(2026, 7, 9),
            AlterarDescricao = true, Descricao = "Nova",
            AlterarCategoria = true, Categoria = "Guitarra",
            AlterarValor = true, Valor = -20.5m,
            AlterarObservacao = true, Observacao = "obs",
            AlterarTipo = true, Tipo = "Parcelado",
        }, Ct);

        Assert.Equal((new DateTime(2026, 7, 9), "Nova", "Guitarra", -20.5m, "obs", "Parcelado"),
            (l!.Data!.Value, l.Descricao, l.Categoria, l.Valor, l.Observacao, l.Tipo));
    }

    [Fact]
    public async Task Atualizar_ParcialENulos_MantemOsDemaisCampos()
    {
        var id = await Db.InserirLancamentoAsync("2026-06-05", "Pet", 1, "a", "b");

        var l = await Repo.AtualizarAsync(id.ToString(), new LancamentoAlteracoes
        {
            AlterarDescricao = true, Descricao = null, AlterarObservacao = true, Observacao = null,
        }, Ct);

        Assert.Null(l!.Descricao);
        Assert.Null(l.Observacao);
        Assert.Equal(("Pet", 1m, new DateTime(2026, 6, 5)), (l.Categoria, l.Valor, l.Data!.Value));
    }

    [Fact]
    public async Task Atualizar_Inexistente_RetornaNull() =>
        Assert.Null(await Repo.AtualizarAsync(Guid.NewGuid().ToString(), new LancamentoAlteracoes { AlterarValor = true, Valor = 1 }, Ct));

    [Fact]
    public async Task Excluir_RemoveEInexistenteNaoFalha()
    {
        var id = await Db.InserirLancamentoAsync("2026-06-05", "Pet", 1);

        await Repo.ExcluirAsync(id.ToString(), Ct);
        await Repo.ExcluirAsync(Guid.NewGuid().ToString(), Ct);

        Assert.Equal(0L, await Db.ConsultarUmAsync<long>("SELECT COUNT(*) FROM lancamentos"));
    }
}
