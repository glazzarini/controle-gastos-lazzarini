using System.Data;
using ControleGastos.Core.Entities;
using ControleGastos.Core.Interfaces;
using Dapper;

namespace ControleGastos.Core.Repositories;

public sealed class LancamentoRepository(IDbConnectionFactory factory) : ILancamentoRepository
{
    private const string Colunas =
        "id AS Id, data AS Data, descricao AS Descricao, categoria AS Categoria, valor AS Valor, " +
        "observacao AS Observacao, tipo AS Tipo, created_at AS CreatedAt";

    private const string SqlListarTodos =
        "SELECT " + Colunas + " FROM lancamentos ORDER BY data DESC, created_at DESC";

    private const string SqlListarPeriodo =
        "SELECT " + Colunas + " FROM lancamentos " +
        "WHERE data BETWEEN CAST(@Inicio AS date) AND CAST(@Fim AS date) " +
        "ORDER BY data DESC, created_at DESC";

    private const string SqlObter =
        "SELECT " + Colunas + " FROM lancamentos WHERE id = CAST(@Id AS uuid)";

    private const string SqlInserir =
        "INSERT INTO lancamentos (data, descricao, categoria, valor, observacao, tipo) " +
        "VALUES (CAST(@Data AS date), @Descricao, @Categoria, @Valor, @Observacao, @Tipo) RETURNING id";

    private const string SqlExcluir = "DELETE FROM lancamentos WHERE id = CAST(@Id AS uuid)";

    public async Task<IReadOnlyList<Lancamento>> ListarAsync(DateTime? inicio, DateTime? fim, CancellationToken ct)
    {
        await using var conexao = await factory.AbrirAsync(ct);
        var comando = inicio is null || fim is null
            ? new CommandDefinition(SqlListarTodos, cancellationToken: ct)
            : new CommandDefinition(SqlListarPeriodo, new { Inicio = inicio, Fim = fim }, cancellationToken: ct);
        return (await conexao.QueryAsync<Lancamento>(comando)).AsList();
    }

    public async Task<Lancamento?> ObterPorIdAsync(string id, CancellationToken ct)
    {
        await using var conexao = await factory.AbrirAsync(ct);
        return await conexao.QuerySingleOrDefaultAsync<Lancamento>(
            new CommandDefinition(SqlObter, new { Id = id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<Guid>> InserirAsync(IReadOnlyList<NovoLancamento> lancamentos, CancellationToken ct)
    {
        await using var conexao = await factory.AbrirAsync(ct);
        await using var transacao = await conexao.BeginTransactionAsync(ct);
        var ids = new List<Guid>(lancamentos.Count);
        foreach (var l in lancamentos)
        {
            ids.Add(await conexao.ExecuteScalarAsync<Guid>(new CommandDefinition(
                SqlInserir,
                new { l.Data, l.Descricao, l.Categoria, l.Valor, l.Observacao, l.Tipo },
                transacao,
                cancellationToken: ct)));
        }
        await transacao.CommitAsync(ct);
        return ids;
    }

    /// <remarks>O chamador garante que <paramref name="alteracoes"/> não é vazia.</remarks>
    public async Task<Lancamento?> AtualizarAsync(string id, LancamentoAlteracoes alteracoes, CancellationToken ct)
    {
        var sets = new List<string>();
        var parametros = new DynamicParameters();
        parametros.Add("Id", id);
        if (alteracoes.AlterarData)
        {
            sets.Add("data = CAST(@Data AS date)");
            parametros.Add("Data", alteracoes.Data);
        }
        if (alteracoes.AlterarDescricao)
        {
            sets.Add("descricao = @Descricao");
            parametros.Add("Descricao", alteracoes.Descricao, DbType.String);
        }
        if (alteracoes.AlterarCategoria)
        {
            sets.Add("categoria = @Categoria");
            parametros.Add("Categoria", alteracoes.Categoria);
        }
        if (alteracoes.AlterarValor)
        {
            sets.Add("valor = @Valor");
            parametros.Add("Valor", alteracoes.Valor);
        }
        if (alteracoes.AlterarObservacao)
        {
            sets.Add("observacao = @Observacao");
            parametros.Add("Observacao", alteracoes.Observacao, DbType.String);
        }
        if (alteracoes.AlterarTipo)
        {
            sets.Add("tipo = @Tipo");
            parametros.Add("Tipo", alteracoes.Tipo);
        }

        var sql = "UPDATE lancamentos SET " + string.Join(", ", sets) +
                  " WHERE id = CAST(@Id AS uuid) RETURNING " + Colunas;

        await using var conexao = await factory.AbrirAsync(ct);
        return await conexao.QuerySingleOrDefaultAsync<Lancamento>(
            new CommandDefinition(sql, parametros, cancellationToken: ct));
    }

    public async Task ExcluirAsync(string id, CancellationToken ct)
    {
        await using var conexao = await factory.AbrirAsync(ct);
        await conexao.ExecuteAsync(new CommandDefinition(SqlExcluir, new { Id = id }, cancellationToken: ct));
    }
}
