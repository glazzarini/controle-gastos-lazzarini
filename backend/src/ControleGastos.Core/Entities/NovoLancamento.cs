namespace ControleGastos.Core.Entities;

/// <summary>Dados de inserção de um lançamento (uma parcela).</summary>
public sealed record NovoLancamento(
    DateTime Data,
    string Descricao,
    string Categoria,
    decimal Valor,
    string? Observacao,
    string Tipo);
