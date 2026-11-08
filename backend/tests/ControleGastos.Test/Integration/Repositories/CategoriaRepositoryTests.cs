using ControleGastos.Core.Exceptions;
using ControleGastos.Core.Repositories;
using ControleGastos.Test.Fixtures;
using Npgsql;

namespace ControleGastos.Test.Integration.Repositories;

public sealed class CategoriaRepositoryTests(PostgresFixture db) : IntegracaoBase(db)
{
    private CategoriaRepository Repo => new(new NpgsqlConnectionFactory(Db.ConnectionString));

    private Task<string> NomeDaCategoria(Guid id) =>
        Db.ConsultarUmAsync<string>("SELECT nome FROM categorias WHERE id = @id", new { id });

    [Fact]
    public async Task Listar_OrdenaPorNome()
    {
        await Db.InserirCategoriaAsync("Pet", 450);
        await Db.InserirCategoriaAsync("Alimentacao fora", 2000);

        var lista = await Repo.ListarAsync(Ct);

        Assert.Equal(["Alimentacao fora", "Pet"], lista.Select(c => c.Nome));
        Assert.Equal(2000m, lista[0].Teto);
        Assert.NotNull(lista[0].CreatedAt);
    }

    [Fact]
    public async Task ObterPorId()
    {
        var id = await Db.InserirCategoriaAsync("Pet", 450);

        Assert.Equal("Pet", (await Repo.ObterPorIdAsync(id.ToString(), Ct))!.Nome);
        Assert.Null(await Repo.ObterPorIdAsync(Guid.NewGuid().ToString(), Ct));
    }

    [Fact]
    public async Task Criar_Insere()
    {
        var c = await Repo.CriarAsync("Nova", 100.5m, Ct);

        Assert.NotEqual(Guid.Empty, c.Id);
        Assert.Equal(("Nova", 100.5m), (c.Nome, c.Teto));
    }

    [Fact]
    public async Task Criar_NomeDuplicado_Conflito()
    {
        await Db.InserirCategoriaAsync("Pet", 450);
        var erro = await Assert.ThrowsAsync<ConflitoException>(() => Repo.CriarAsync("Pet", 1, Ct));
        Assert.Equal(CategoriaRepository.MensagemNomeDuplicado, erro.Message);
    }

    [Fact]
    public async Task Criar_OutroErro_Propaga()
    {
        var erro = await Assert.ThrowsAsync<PostgresException>(() => Repo.CriarAsync("X", 100_000_000_000m, Ct));
        Assert.Equal(PostgresErrorCodes.NumericValueOutOfRange, erro.SqlState);
    }

    [Fact]
    public async Task Atualizar_RenomeiaEPropagaParaOHistorico()
    {
        var id = await Db.InserirCategoriaAsync("Pet", 450);
        await Db.InserirLancamentoAsync("2026-06-01", "Pet", 1);
        await Db.InserirLancamentoAsync("2027-01-01", "Pet", 2);
        await Db.InserirLancamentoAsync("2026-06-01", "Guitarra", 3);

        var resultado = await Repo.AtualizarAsync(id.ToString(), "Animais", 500, "Pet", Ct);

        Assert.Equal(("Animais", 500m), (resultado!.Value.Categoria.Nome, resultado.Value.Categoria.Teto));
        Assert.Equal(2, resultado.Value.LancamentosAtualizados);
        Assert.Equal(2L, await Db.ConsultarUmAsync<long>("SELECT COUNT(*) FROM lancamentos WHERE categoria = 'Animais'"));
        Assert.Equal(1L, await Db.ConsultarUmAsync<long>("SELECT COUNT(*) FROM lancamentos WHERE categoria = 'Guitarra'"));
    }

    [Fact]
    public async Task Atualizar_SoTeto_NaoMexeNosLancamentos()
    {
        var id = await Db.InserirCategoriaAsync("Pet", 450);
        await Db.InserirLancamentoAsync("2026-06-01", "Pet", 1);

        var resultado = await Repo.AtualizarAsync(id.ToString(), null, 10, "Pet", Ct);

        Assert.Equal(0, resultado!.Value.LancamentosAtualizados);
        Assert.Equal(10m, resultado.Value.Categoria.Teto);
    }

    [Fact]
    public async Task Atualizar_MesmoNome_NaoPropaga()
    {
        var id = await Db.InserirCategoriaAsync("Pet", 450);
        await Db.InserirLancamentoAsync("2026-06-01", "Pet", 1);

        var resultado = await Repo.AtualizarAsync(id.ToString(), "Pet", null, "Pet", Ct);

        Assert.Equal(0, resultado!.Value.LancamentosAtualizados);
    }

    [Fact]
    public async Task Atualizar_Inexistente_RetornaNull() =>
        Assert.Null(await Repo.AtualizarAsync(Guid.NewGuid().ToString(), "X", null, "Y", Ct));

    [Fact]
    public async Task Atualizar_NomeDuplicado_ConflitoSemAlterarNada()
    {
        var id = await Db.InserirCategoriaAsync("Pet", 450);
        await Db.InserirCategoriaAsync("Guitarra", 300);

        await Assert.ThrowsAsync<ConflitoException>(() => Repo.AtualizarAsync(id.ToString(), "Guitarra", null, "Pet", Ct));

        Assert.Equal("Pet", await NomeDaCategoria(id));
    }

    [Fact]
    public async Task Atualizar_OutroErro_Propaga()
    {
        var erro = await Assert.ThrowsAsync<PostgresException>(() => Repo.AtualizarAsync("x", "X", null, "Y", Ct));
        Assert.Equal(PostgresErrorCodes.InvalidTextRepresentation, erro.SqlState);
    }

    [Fact]
    public async Task Atualizar_FalhaNaPropagacao_DesfazARenomeacao()
    {
        // Transação: se o UPDATE em lancamentos falhar, a categoria não pode ficar renomeada.
        var id = await Db.InserirCategoriaAsync("Pet", 450);
        await Db.InserirLancamentoAsync("2026-06-01", "Pet", 1);

        await using (await Db.FalharEmAsync("lancamentos", "UPDATE"))
        {
            await Assert.ThrowsAsync<PostgresException>(() => Repo.AtualizarAsync(id.ToString(), "Animais", 1, "Pet", Ct));
        }

        Assert.Equal("Pet", await NomeDaCategoria(id));
        Assert.Equal(450m, await Db.ConsultarUmAsync<decimal>("SELECT teto FROM categorias WHERE id = @id", new { id }));
        Assert.Equal(1L, await Db.ConsultarUmAsync<long>("SELECT COUNT(*) FROM lancamentos WHERE categoria = 'Pet'"));
    }

    [Fact]
    public async Task ContarLancamentos()
    {
        await Db.InserirLancamentoAsync("2026-06-01", "Pet", 1);
        await Db.InserirLancamentoAsync("2026-07-01", "Pet", 1);

        Assert.Equal(2, await Repo.ContarLancamentosAsync("Pet", Ct));
        Assert.Equal(0, await Repo.ContarLancamentosAsync("Outra", Ct));
    }

    [Fact]
    public async Task Excluir()
    {
        var id = await Db.InserirCategoriaAsync("Pet", 450);
        await Repo.ExcluirAsync(id.ToString(), Ct);
        Assert.Empty(await Repo.ListarAsync(Ct));
    }
}
