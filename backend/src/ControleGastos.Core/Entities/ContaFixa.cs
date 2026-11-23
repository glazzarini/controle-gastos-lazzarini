namespace ControleGastos.Core.Entities;

/// <summary>Linha da tabela <c>contas_fixas</c>.</summary>
public sealed class ContaFixa
{
    public Guid Id { get; init; }
    public string Nome { get; init; } = string.Empty;
    public decimal Valor { get; init; }
    public DateTime? CreatedAt { get; init; }
}
