using ControleGastos.Core.Entities;
using ControleGastos.Core.Interfaces;
using Dapper;

namespace ControleGastos.Core.Repositories;

public sealed class ConfigRepository(IDbConnectionFactory factory) : IConfigRepository
{
    private const string SqlListar = "SELECT chave AS Chave, valor AS Valor FROM config";

    private const string SqlUpsert =
        "INSERT INTO config (chave, valor) VALUES (@Chave, @Valor) " +
        "ON CONFLICT (chave) DO UPDATE SET valor = EXCLUDED.valor";

    public async Task<IReadOnlyList<ConfigItem>> ListarAsync(CancellationToken ct)
    {
        await using var conexao = await factory.AbrirAsync(ct);
        return (await conexao.QueryAsync<ConfigItem>(new CommandDefinition(SqlListar, cancellationToken: ct))).AsList();
    }

    public async Task SalvarAsync(IReadOnlyList<ConfigItem> itens, CancellationToken ct)
    {
        await using var conexao = await factory.AbrirAsync(ct);
        await using var transacao = await conexao.BeginTransactionAsync(ct);
        foreach (var item in itens)
        {
            await conexao.ExecuteAsync(new CommandDefinition(
                SqlUpsert, new { item.Chave, item.Valor }, transacao, cancellationToken: ct));
        }
        await transacao.CommitAsync(ct);
    }
}
