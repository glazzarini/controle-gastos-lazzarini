namespace ControleGastos.Core.Entities;

/// <summary>
/// Campos a alterar num lançamento. Cada campo só é aplicado quando o respectivo
/// <c>Alterar*</c> é verdadeiro (equivalente ao "só atualiza os campos enviados" da API atual).
/// </summary>
public sealed record LancamentoAlteracoes
{
    public bool AlterarData { get; init; }
    public DateTime Data { get; init; }
    public bool AlterarDescricao { get; init; }
    public string? Descricao { get; init; }
    public bool AlterarCategoria { get; init; }
    public string Categoria { get; init; } = string.Empty;
    public bool AlterarValor { get; init; }
    public decimal Valor { get; init; }
    public bool AlterarObservacao { get; init; }
    public string? Observacao { get; init; }
    public bool AlterarTipo { get; init; }
    public string Tipo { get; init; } = string.Empty;

    public bool Vazia =>
        !AlterarData && !AlterarDescricao && !AlterarCategoria &&
        !AlterarValor && !AlterarObservacao && !AlterarTipo;
}
