namespace ControleGastos.Core.Entities;

/// <summary>Linha da tabela <c>config</c> (pares chave/valor em texto).</summary>
public sealed class ConfigItem
{
    public string Chave { get; init; } = string.Empty;
    public string Valor { get; init; } = string.Empty;
}
