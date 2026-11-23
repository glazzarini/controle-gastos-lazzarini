using ControleGastos.Core.Dtos.Responses;
using ControleGastos.Core.Entities;

namespace ControleGastos.Core.Mappings;

/// <summary>Mapeamento manual entidade → DTO (sem AutoMapper).</summary>
public static class Mappings
{
    public static LancamentoResponse ToResponse(this Lancamento e) => new(
        e.Id,
        e.Data is { } d ? DateOnly.FromDateTime(d) : null,
        e.Descricao,
        e.Categoria,
        e.Valor,
        e.Observacao,
        e.Tipo,
        e.CreatedAt);

    public static CategoriaResponse ToResponse(this Categoria e) => new(e.Id, e.Nome, e.Teto, e.CreatedAt);

    public static ContaFixaResponse ToResponse(this ContaFixa e) => new(e.Id, e.Nome, e.Valor, e.CreatedAt);

    public static ContaFixaResumoResponse ToResumo(this ContaFixa e) => new(e.Id, e.Nome, e.Valor);
}
