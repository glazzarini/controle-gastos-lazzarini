namespace ControleGastos.Core.Entities;

/// <summary>Linha da tabela <c>categorias</c>.</summary>
public sealed class Categoria
{
    public Guid Id { get; init; }
    public string Nome { get; init; } = string.Empty;
    public decimal Teto { get; init; }
    public DateTime? CreatedAt { get; init; }
}
