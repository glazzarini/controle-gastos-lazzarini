using ControleGastos.Core.Entities;
using ControleGastos.Core.Interfaces;
using Dapper;

namespace ControleGastos.Core.Repositories;

public sealed class ContaFixaRepository(IDbConnectionFactory factory) : IContaFixaRepository
{
    private const string Colunas = "id AS Id, nome AS Nome, valor AS Valor, created_at AS CreatedAt";

    private const string SqlListar = "SELECT " + Colunas + " FROM contas_fixas ORDER BY created_at ASC";

    private const string SqlObter = "SELECT " + Colunas + " FROM contas_fixas WHERE id = CAST(@Id AS uuid)";

    private const string SqlCriar =
        "INSERT INTO contas_fixas (nome, valor) VALUES (@Nome, @Valor) RETURNING " + Colunas;

    private const string SqlExcluir = "DELETE FROM contas_fixas WHERE id = CAST(@Id AS uuid)";

    public async Task<IReadOnlyList<ContaFixa>> ListarAsync(CancellationToken ct)
    {
        await using var conexao = await factory.AbrirAsync(ct);
        return (await conexao.QueryAsync<ContaFixa>(new CommandDefinition(SqlListar, cancellationToken: ct))).AsList();
    }

    public async Task<ContaFixa?> ObterPorIdAsync(string id, CancellationToken ct)
    {
        await using var conexao = await factory.AbrirAsync(ct);
        return await conexao.QuerySingleOrDefaultAsync<ContaFixa>(
            new CommandDefinition(SqlObter, new { Id = id }, cancellationToken: ct));
    }

    public async Task<ContaFixa> CriarAsync(string nome, decimal valor, CancellationToken ct)
    {
        await using var conexao = await factory.AbrirAsync(ct);
        return await conexao.QuerySingleAsync<ContaFixa>(
            new CommandDefinition(SqlCriar, new { Nome = nome, Valor = valor }, cancellationToken: ct));
    }

    /// <remarks>O chamador garante que <paramref name="nome"/> ou <paramref name="valor"/> foi informado.</remarks>
    public async Task<ContaFixa?> AtualizarAsync(string id, string? nome, decimal? valor, CancellationToken ct)
    {
        var sets = new List<string>();
        var parametros = new DynamicParameters();
        parametros.Add("Id", id);
        if (nome is not null)
        {
            sets.Add("nome = @Nome");
            parametros.Add("Nome", nome);
        }
        if (valor is not null)
        {
            sets.Add("valor = @Valor");
            parametros.Add("Valor", valor.Value);
        }
        var sql = "UPDATE contas_fixas SET " + string.Join(", ", sets) +
                  " WHERE id = CAST(@Id AS uuid) RETURNING " + Colunas;

        await using var conexao = await factory.AbrirAsync(ct);
        return await conexao.QuerySingleOrDefaultAsync<ContaFixa>(
            new CommandDefinition(sql, parametros, cancellationToken: ct));
    }

    public async Task ExcluirAsync(string id, CancellationToken ct)
    {
        await using var conexao = await factory.AbrirAsync(ct);
        await conexao.ExecuteAsync(new CommandDefinition(SqlExcluir, new { Id = id }, cancellationToken: ct));
    }
}
