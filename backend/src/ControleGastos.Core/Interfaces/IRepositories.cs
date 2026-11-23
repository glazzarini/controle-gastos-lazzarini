using System.Data.Common;
using ControleGastos.Core.Entities;

namespace ControleGastos.Core.Interfaces;

public interface IDbConnectionFactory
{
    /// <summary>Abre uma conexão nova (do pool do Npgsql) com o Postgres.</summary>
    Task<DbConnection> AbrirAsync(CancellationToken ct);
}

public interface ILancamentoRepository
{
    Task<IReadOnlyList<Lancamento>> ListarAsync(DateTime? inicio, DateTime? fim, CancellationToken ct);
    Task<Lancamento?> ObterPorIdAsync(string id, CancellationToken ct);
    Task<IReadOnlyList<Guid>> InserirAsync(IReadOnlyList<NovoLancamento> lancamentos, CancellationToken ct);
    Task<Lancamento?> AtualizarAsync(string id, LancamentoAlteracoes alteracoes, CancellationToken ct);
    Task ExcluirAsync(string id, CancellationToken ct);
}

public interface ICategoriaRepository
{
    Task<IReadOnlyList<Categoria>> ListarAsync(CancellationToken ct);
    Task<Categoria?> ObterPorIdAsync(string id, CancellationToken ct);

    /// <summary>Insere; lança <see cref="Exceptions.ConflitoException"/> em nome duplicado.</summary>
    Task<Categoria> CriarAsync(string nome, decimal teto, CancellationToken ct);

    /// <summary>
    /// Atualiza nome/teto e, se o nome mudou, propaga para <c>lancamentos</c> — tudo numa transação.
    /// Retorna <c>null</c> se a categoria não existir. Lança <see cref="Exceptions.ConflitoException"/> em nome duplicado.
    /// </summary>
    Task<(Categoria Categoria, int LancamentosAtualizados)?> AtualizarAsync(
        string id, string? nome, decimal? teto, string nomeAntigo, CancellationToken ct);

    Task<int> ContarLancamentosAsync(string nomeCategoria, CancellationToken ct);
    Task ExcluirAsync(string id, CancellationToken ct);
}

public interface IContaFixaRepository
{
    Task<IReadOnlyList<ContaFixa>> ListarAsync(CancellationToken ct);
    Task<ContaFixa?> ObterPorIdAsync(string id, CancellationToken ct);
    Task<ContaFixa> CriarAsync(string nome, decimal valor, CancellationToken ct);
    Task<ContaFixa?> AtualizarAsync(string id, string? nome, decimal? valor, CancellationToken ct);
    Task ExcluirAsync(string id, CancellationToken ct);
}

public interface IConfigRepository
{
    Task<IReadOnlyList<ConfigItem>> ListarAsync(CancellationToken ct);
    Task SalvarAsync(IReadOnlyList<ConfigItem> itens, CancellationToken ct);
}
