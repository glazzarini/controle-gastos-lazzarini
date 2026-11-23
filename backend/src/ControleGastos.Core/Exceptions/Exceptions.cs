namespace ControleGastos.Core.Exceptions;

/// <summary>Erro de regra/validação; o middleware da API converte em status HTTP + <c>{ error }</c>.</summary>
public abstract class ControleGastosException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;

    /// <summary>Campos adicionais devolvidos junto com <c>error</c> (ex.: <c>lancamentos_em_uso</c>).</summary>
    public virtual IReadOnlyDictionary<string, object> Extras { get; } = new Dictionary<string, object>();
}

/// <summary>400.</summary>
public sealed class ValidacaoException(string message) : ControleGastosException(400, message);

/// <summary>404.</summary>
public sealed class NaoEncontradoException(string message) : ControleGastosException(404, message);

/// <summary>409.</summary>
public sealed class ConflitoException(string message, IReadOnlyDictionary<string, object>? extras = null)
    : ControleGastosException(409, message)
{
    public override IReadOnlyDictionary<string, object> Extras { get; } = extras ?? new Dictionary<string, object>();
}
