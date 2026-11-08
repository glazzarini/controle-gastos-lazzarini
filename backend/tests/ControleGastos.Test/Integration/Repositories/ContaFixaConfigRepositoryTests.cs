using System.Data.Common;
using ControleGastos.Core.Entities;
using ControleGastos.Core.Repositories;
using ControleGastos.Test.Fixtures;
using Npgsql;

namespace ControleGastos.Test.Integration.Repositories;

public sealed class ContaFixaRepositoryTests(PostgresFixture db) : IntegracaoBase(db)
{
    private ContaFixaRepository Repo => new(new NpgsqlConnectionFactory(Db.ConnectionString));

    [Fact]
    public async Task Listar_OrdenaPorCreatedAt()
    {
        await Db.InserirContaFixaAsync("Segunda", 2, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));
        await Db.InserirContaFixaAsync("Primeira", 1, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var lista = await Repo.ListarAsync(Ct);

        Assert.Equal(["Primeira", "Segunda"], lista.Select(c => c.Nome));
        Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), lista[0].CreatedAt);
    }

    [Fact]
    public async Task ObterCriarAtualizarExcluir()
    {
        var criada = await Repo.CriarAsync("Internet", 120.9m, Ct);
        Assert.Equal(("Internet", 120.9m), (criada.Nome, criada.Valor));
        var id = criada.Id.ToString();

        Assert.Equal(criada.Id, (await Repo.ObterPorIdAsync(id, Ct))!.Id);
        Assert.Null(await Repo.ObterPorIdAsync(Guid.NewGuid().ToString(), Ct));

        Assert.Equal(("Fibra", 120.9m), Par(await Repo.AtualizarAsync(id, "Fibra", null, Ct)));
        Assert.Equal(("Fibra", 99m), Par(await Repo.AtualizarAsync(id, null, 99, Ct)));
        Assert.Equal(("Net", 1m), Par(await Repo.AtualizarAsync(id, "Net", 1, Ct)));
        Assert.Null(await Repo.AtualizarAsync(Guid.NewGuid().ToString(), "x", null, Ct));

        await Repo.ExcluirAsync(id, Ct);
        Assert.Empty(await Repo.ListarAsync(Ct));

        static (string, decimal) Par(ContaFixa? c) => (c!.Nome, c.Valor);
    }
}

public sealed class ConfigRepositoryTests(PostgresFixture db) : IntegracaoBase(db)
{
    private ConfigRepository Repo => new(new NpgsqlConnectionFactory(Db.ConnectionString));

    [Fact]
    public async Task Listar()
    {
        await Db.InserirConfigAsync("renda_guilherme", "20000");

        var item = Assert.Single(await Repo.ListarAsync(Ct));

        Assert.Equal(("renda_guilherme", "20000"), (item.Chave, item.Valor));
    }

    [Fact]
    public async Task Salvar_InsereNovaEAtualizaExistente()
    {
        await Db.InserirConfigAsync("renda_guilherme", "20000");

        await Repo.SalvarAsync([
            new ConfigItem { Chave = "renda_guilherme", Valor = "21000" },
            new ConfigItem { Chave = "renda_esposa", Valor = "15000.5" },
        ], Ct);

        var itens = (await Repo.ListarAsync(Ct)).OrderBy(i => i.Chave, StringComparer.Ordinal).Select(i => (i.Chave, i.Valor));
        Assert.Equal([("renda_esposa", "15000.5"), ("renda_guilherme", "21000")], itens);
    }
}

public sealed class NpgsqlConnectionFactoryTests(PostgresFixture db) : IntegracaoBase(db)
{
    [Fact]
    public async Task Abrir_RetornaConexaoAberta()
    {
        await using DbConnection conexao = await new NpgsqlConnectionFactory(Db.ConnectionString).AbrirAsync(Ct);
        Assert.Equal(System.Data.ConnectionState.Open, conexao.State);
    }

    [Fact]
    public async Task Abrir_BancoInacessivel_Lanca() =>
        await Assert.ThrowsAsync<NpgsqlException>(
            () => new NpgsqlConnectionFactory(PostgresFixture.ConnectionStringInacessivel).AbrirAsync(Ct));

    [Fact]
    public async Task Abrir_ConnectionStringVazia_Lanca() =>
        await Assert.ThrowsAnyAsync<Exception>(() => new NpgsqlConnectionFactory(string.Empty).AbrirAsync(Ct));

    [Theory]
    [InlineData("Host=h;Username=u;Password=p;SSL Mode=Require")]
    [InlineData("Host=h;Gss Encryption Mode=Prefer")]
    [InlineData("")]
    public void Normalizar_DesligaGssEMantemORestante(string original)
    {
        var resultado = new NpgsqlConnectionStringBuilder(NpgsqlConnectionFactory.Normalizar(original));
        var esperado = new NpgsqlConnectionStringBuilder(original);

        Assert.Equal(GssEncryptionMode.Disable, resultado.GssEncryptionMode);
        Assert.Equal(esperado.Host, resultado.Host);
        Assert.Equal(esperado.Password, resultado.Password);
        Assert.Equal(esperado.SslMode, resultado.SslMode);
    }
}
