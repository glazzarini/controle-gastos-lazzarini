namespace ControleGastos.Core.Dtos;

/// <summary>Estado de um campo recebido no body JSON.</summary>
public enum EstadoCampo
{
    /// <summary>Campo não enviado (equivale a <c>undefined</c> no JS).</summary>
    Ausente = 0,
    /// <summary>Enviado como <c>null</c>.</summary>
    Nulo,
    /// <summary>Enviado como string vazia (<c>""</c>).</summary>
    Vazio,
    /// <summary>Enviado com tipo/formato que não pode ser convertido.</summary>
    Invalido,
    /// <summary>Enviado com valor válido.</summary>
    Valor,
}

/// <summary>
/// Campo opcional de request, preservando a diferença entre ausente, nulo, vazio e inválido —
/// necessária para reproduzir as regras de validação da API atual (<c>!== undefined</c>, truthy, <c>parseFloat</c>).
/// </summary>
public readonly record struct CampoOpcional<T>(EstadoCampo Estado, T? Valor = default)
{
    /// <summary>Campo enviado (qualquer estado diferente de <see cref="EstadoCampo.Ausente"/>).</summary>
    public bool Presente => Estado != EstadoCampo.Ausente;

    /// <summary>Campo com valor válido.</summary>
    public bool TemValor => Estado == EstadoCampo.Valor;
}

/// <summary>Fábricas de <see cref="CampoOpcional{T}"/>.</summary>
public static class CampoOpcional
{
    public static CampoOpcional<T> Ausente<T>() => default;
    public static CampoOpcional<T> Nulo<T>() => new(EstadoCampo.Nulo);
    public static CampoOpcional<T> Vazio<T>() => new(EstadoCampo.Vazio);
    public static CampoOpcional<T> Invalido<T>() => new(EstadoCampo.Invalido);
    public static CampoOpcional<T> De<T>(T valor) => new(EstadoCampo.Valor, valor);
}
