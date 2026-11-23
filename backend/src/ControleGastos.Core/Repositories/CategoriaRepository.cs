using ControleGastos.Core.Entities;
using ControleGastos.Core.Exceptions;
using ControleGastos.Core.Interfaces;
using Dapper;
using Npgsql;

namespace ControleGastos.Core.Repositories;

public sealed class CategoriaRepository(IDbConnectionFactory factory) : ICategoriaRepository
{
    public const string MensagemNomeDuplicado = "Já existe uma categoria com esse nome";

    private const string Colunas = "id AS Id, nome AS Nome, teto AS Teto, created_at AS CreatedAt";

    private const string SqlListar = "SELECT " + Colunas + " FROM categorias ORDER BY nome ASC";

    private const string SqlObter = "SELECT " + Colunas + " FROM categorias WHERE id = CAST(@Id AS uuid)";

    private const string SqlCriar =
        "INSERT INTO categorias (nome, teto) VALUES (@Nome, @Teto) RETURNING " + Colunas;

    private const string SqlPropagarRenomeacao =
        "UPDATE lancamentos SET categoria = @Novo WHERE categoria = @Antigo";

    private const string SqlContarLancamentos = "SELECT COUNT(*) FROM lancamentos WHERE categoria = @Nome";

    private const string SqlExcluir = "DELETE FROM categorias WHERE id = CAST(@Id AS uuid)";

    public async Task<IReadOnlyList<Categoria>> ListarAsync(CancellationToken ct)
    {
        await using var conexao = await factory.AbrirAsync(ct);
        return (await conexao.QueryAsync<Categoria>(new CommandDefinition(SqlListar, cancellationToken: ct))).AsList();
    }

    public async Task<Categoria?> ObterPorIdAsync(string id, CancellationToken ct)
    {
        await using var conexao = await factory.AbrirAsync(ct);
        return await conexao.QuerySingleOrDefaultAsync<Categoria>(
            new CommandDefinition(SqlObter, new { Id = id }, cancellationToken: ct));
    }

    public async Task<Categoria> CriarAsync(string nome, decimal teto, CancellationToken ct)
    {
        await using var conexao = await factory.AbrirAsync(ct);
        try
        {
            return await conexao.QuerySingleAsync<Categoria>(
                new CommandDefinition(SqlCriar, new { Nome = nome, Teto = teto }, cancellationToken: ct));
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new ConflitoException(MensagemNomeDuplicado);
        }
    }

    /// <remarks>O chamador garante que <paramref name="nome"/> ou <paramref name="teto"/> foi informado.</remarks>
    public async Task<(Categoria Categoria, int LancamentosAtualizados)?> AtualizarAsync(
        string id, string? nome, decimal? teto, string nomeAntigo, CancellationToken ct)
    {
        var sets = new List<string>();
        var parametros = new DynamicParameters();
        parametros.Add("Id", id);
        if (nome is not null)
        {
            sets.Add("nome = @Nome");
            parametros.Add("Nome", nome);
        }
        if (teto is not null)
        {
            sets.Add("teto = @Teto");
            parametros.Add("Teto", teto.Value);
        }
        var sql = "UPDATE categorias SET " + string.Join(", ", sets) +
                  " WHERE id = CAST(@Id AS uuid) RETURNING " + Colunas;

        await using var conexao = await factory.AbrirAsync(ct);
        await using var transacao = await conexao.BeginTransactionAsync(ct);

        Categoria? categoria;
        try
        {
            categoria = await conexao.QuerySingleOrDefaultAsync<Categoria>(
                new CommandDefinition(sql, parametros, transacao, cancellationToken: ct));
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new ConflitoException(MensagemNomeDuplicado);
        }

        if (categoria is null)
        {
            return null;
        }

        // Job de propagação: renomeia a categoria em todo o histórico de lançamentos (mesma transação).
        var atualizados = 0;
        if (nome is not null && nome != nomeAntigo)
        {
            atualizados = await conexao.ExecuteAsync(new CommandDefinition(
                SqlPropagarRenomeacao, new { Novo = nome, Antigo = nomeAntigo }, transacao, cancellationToken: ct));
        }

        await transacao.CommitAsync(ct);
        return (categoria, atualizados);
    }

    public async Task<int> ContarLancamentosAsync(string nomeCategoria, CancellationToken ct)
    {
        await using var conexao = await factory.AbrirAsync(ct);
        return (int)await conexao.ExecuteScalarAsync<long>(
            new CommandDefinition(SqlContarLancamentos, new { Nome = nomeCategoria }, cancellationToken: ct));
    }

    public async Task ExcluirAsync(string id, CancellationToken ct)
    {
        await using var conexao = await factory.AbrirAsync(ct);
        await conexao.ExecuteAsync(new CommandDefinition(SqlExcluir, new { Id = id }, cancellationToken: ct));
    }
}
