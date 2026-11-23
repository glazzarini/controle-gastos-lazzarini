namespace ControleGastos.Core.Entities;

/// <summary>Linha da tabela <c>lancamentos</c>. <see cref="Valor"/> negativo representa crédito na fatura.</summary>
public sealed class Lancamento
{
    public Guid Id { get; init; }
    public DateTime? Data { get; init; }
    public string? Descricao { get; init; }
    public string Categoria { get; init; } = string.Empty;
    public decimal Valor { get; init; }
    public string? Observacao { get; init; }
    public string? Tipo { get; init; }
    public DateTime? CreatedAt { get; init; }
}
