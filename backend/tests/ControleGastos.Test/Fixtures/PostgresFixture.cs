using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using Testcontainers.PostgreSql;

namespace ControleGastos.Test.Fixtures;

/// <summary>
/// Postgres descartável (Testcontainers) com o mesmo DDL do Supabase
/// (<c>supabase-setup.sql</c> + <c>supabase-migration-categorias-contas-fixas.sql</c>).
/// Também expõe a API (WebApplicationFactory) apontando para esse banco e uma API com banco inacessível.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    public const string ConnectionStringInacessivel =
        "Host=127.0.0.1;Port=1;Database=postgres;Username=postgres;Password=x;Timeout=1";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public string ConnectionString => _container.GetConnectionString();

    public ApiFactory Api { get; private set; } = null!;

    public ApiFactory ApiSemBanco { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        var pasta = Path.Combine(AppContext.BaseDirectory, "Sql");
        var ddl = string.Join('\n',
            "CREATE ROLE service_role;", // existe no Supabase; os scripts fazem GRANT para ela
            await File.ReadAllTextAsync(Path.Combine(pasta, "supabase-setup.sql")),
            await File.ReadAllTextAsync(Path.Combine(pasta, "supabase-migration-categorias-contas-fixas.sql")));
        await ExecutarAsync(ddl);
        Api = new ApiFactory(ConnectionString);
        ApiSemBanco = new ApiFactory(ConnectionStringInacessivel);
    }

    public async ValueTask DisposeAsync()
    {
        await Api.DisposeAsync();
        await ApiSemBanco.DisposeAsync();
        await _container.DisposeAsync();
    }

    public async Task ExecutarAsync(string sql, object? parametros = null)
    {
        await using var conexao = new NpgsqlConnection(ConnectionString);
        await conexao.ExecuteAsync(sql, parametros);
    }

    public async Task<T> ConsultarUmAsync<T>(string sql, object? parametros = null)
    {
        await using var conexao = new NpgsqlConnection(ConnectionString);
        return await conexao.QuerySingleAsync<T>(sql, parametros);
    }

    public async Task<List<T>> ConsultarAsync<T>(string sql, object? parametros = null)
    {
        await using var conexao = new NpgsqlConnection(ConnectionString);
        return (await conexao.QueryAsync<T>(sql, parametros)).AsList();
    }

    public Task LimparAsync() => ExecutarAsync("TRUNCATE lancamentos, categorias, contas_fixas, config");

    public Task<Guid> InserirLancamentoAsync(
        string dataIso, string categoria, decimal valor, string? descricao = null,
        string? observacao = null, string tipo = "A vista", DateTime? createdAt = null) =>
        ConsultarUmAsync<Guid>(
            "INSERT INTO lancamentos (data, descricao, categoria, valor, observacao, tipo, created_at) " +
            "VALUES (CAST(@dataIso AS date), @descricao, @categoria, @valor, @observacao, @tipo, COALESCE(@createdAt, now())) RETURNING id",
            new { dataIso, descricao, categoria, valor, observacao, tipo, createdAt });

    public Task<Guid> InserirCategoriaAsync(string nome, decimal teto) =>
        ConsultarUmAsync<Guid>("INSERT INTO categorias (nome, teto) VALUES (@nome, @teto) RETURNING id", new { nome, teto });

    public Task<Guid> InserirContaFixaAsync(string nome, decimal valor, DateTime? createdAt = null) =>
        ConsultarUmAsync<Guid>(
            "INSERT INTO contas_fixas (nome, valor, created_at) VALUES (@nome, @valor, COALESCE(@createdAt, now())) RETURNING id",
            new { nome, valor, createdAt });

    public Task InserirConfigAsync(string chave, string valor) =>
        ExecutarAsync("INSERT INTO config (chave, valor) VALUES (@chave, @valor)", new { chave, valor });

    /// <summary>Renomeia a tabela até o dispose — simula "erro do banco" numa query específica.</summary>
    public async Task<IAsyncDisposable> TabelaIndisponivelAsync(string tabela)
    {
        await ExecutarAsync($"ALTER TABLE {tabela} RENAME TO {tabela}_indisponivel");
        return new AoDescartar(() => ExecutarAsync($"ALTER TABLE {tabela}_indisponivel RENAME TO {tabela}"));
    }

    /// <summary>Cria um trigger que falha na operação até o dispose — simula falha num passo específico.</summary>
    public async Task<IAsyncDisposable> FalharEmAsync(string tabela, string operacao, string? condicao = null)
    {
        var nome = $"falha_{tabela}_{operacao}".ToLowerInvariant();
        await ExecutarAsync($"""
            CREATE FUNCTION {nome}() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN
              IF {condicao ?? "true"} THEN RAISE EXCEPTION 'falha simulada em {tabela}'; END IF;
              RETURN COALESCE(NEW, OLD);
            END $$;
            CREATE TRIGGER {nome} BEFORE {operacao} ON {tabela} FOR EACH ROW EXECUTE FUNCTION {nome}();
            """);
        return new AoDescartar(() => ExecutarAsync($"DROP TRIGGER {nome} ON {tabela}; DROP FUNCTION {nome}();"));
    }

    private sealed class AoDescartar(Func<Task> acao) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync() => await acao();
    }
}

/// <summary>API real (Program.cs) apontando para a connection string informada.</summary>
public sealed class ApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuracao) => configuracao.AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:Supabase"] = connectionString }));
        builder.ConfigureLogging(logging => logging.ClearProviders());
    }
}

[CollectionDefinition(Nome)]
public sealed class PostgresColecao : ICollectionFixture<PostgresFixture>
{
    public const string Nome = "Postgres";
}

/// <summary>Base dos testes de integração: banco limpo antes de cada teste.</summary>
[Collection(PostgresColecao.Nome)]
public abstract class IntegracaoBase(PostgresFixture db) : TesteBase, IAsyncLifetime
{
    protected PostgresFixture Db { get; } = db;

    public async ValueTask InitializeAsync() => await Db.LimparAsync();

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
