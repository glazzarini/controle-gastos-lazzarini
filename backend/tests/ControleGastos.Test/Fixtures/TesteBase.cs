using ControleGastos.Core.Dtos;

namespace ControleGastos.Test.Fixtures;

/// <summary>Atalhos comuns aos testes.</summary>
public abstract class TesteBase
{
    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    protected static CampoOpcional<string> Texto(string valor) => CampoOpcional.De(valor);
    protected static CampoOpcional<decimal> Numero(decimal valor) => CampoOpcional.De(valor);
    protected static CampoOpcional<T> Nulo<T>() => CampoOpcional.Nulo<T>();
    protected static CampoOpcional<T> Vazio<T>() => CampoOpcional.Vazio<T>();
    protected static CampoOpcional<T> Invalido<T>() => CampoOpcional.Invalido<T>();
}
