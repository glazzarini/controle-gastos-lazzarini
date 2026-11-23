namespace ControleGastos.Core.Dtos.Responses;

public sealed record ErroResponse(string Error);

public sealed record UpdatedResponse(bool Updated = true);

public sealed record DeletedResponse(bool Deleted = true);

public sealed record LancamentoResponse(
    Guid Id,
    DateOnly? Data,
    string? Descricao,
    string Categoria,
    decimal Valor,
    string? Observacao,
    string? Tipo,
    DateTime? CreatedAt);

public sealed record LancamentosCriadosResponse(int Inserted, IReadOnlyList<Guid> Ids);

public sealed record LancamentoAtualizadoResponse(bool Updated, LancamentoResponse Lancamento);

public sealed record CategoriaResponse(Guid Id, string Nome, decimal Teto, DateTime? CreatedAt);

public sealed record CategoriaAtualizadaResponse(bool Updated, CategoriaResponse Categoria, int LancamentosAtualizados);

public sealed record ContaFixaResponse(Guid Id, string Nome, decimal Valor, DateTime? CreatedAt);

public sealed record ContaFixaAtualizadaResponse(bool Updated, ContaFixaResponse ContaFixa);

public sealed record ContaFixaResumoResponse(Guid Id, string Nome, decimal Valor);

public sealed record ConfigResponse(
    decimal RendaGuilherme,
    decimal RendaEsposa,
    decimal MetaFatura,
    decimal TotalFixos,
    IReadOnlyDictionary<string, decimal> Tetos,
    IReadOnlyList<ContaFixaResumoResponse> ContasFixas);
