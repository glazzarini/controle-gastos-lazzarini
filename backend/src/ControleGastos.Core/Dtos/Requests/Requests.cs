namespace ControleGastos.Core.Dtos.Requests;

public sealed class LancamentoCriarRequest
{
    public CampoOpcional<string> Data { get; init; }
    public CampoOpcional<string> Descricao { get; init; }
    public CampoOpcional<string> Categoria { get; init; }
    public CampoOpcional<decimal> Valor { get; init; }
    public CampoOpcional<string> Observacao { get; init; }
    public CampoOpcional<string> Tipo { get; init; }
    public CampoOpcional<decimal> Parcelas { get; init; }
}

public sealed class LancamentoAtualizarRequest
{
    public CampoOpcional<string> Data { get; init; }
    public CampoOpcional<string> Descricao { get; init; }
    public CampoOpcional<string> Categoria { get; init; }
    public CampoOpcional<decimal> Valor { get; init; }
    public CampoOpcional<string> Observacao { get; init; }
    public CampoOpcional<string> Tipo { get; init; }
}

public sealed class CategoriaCriarRequest
{
    public CampoOpcional<string> Nome { get; init; }
    public CampoOpcional<decimal> Teto { get; init; }
}

public sealed class CategoriaAtualizarRequest
{
    public CampoOpcional<string> Nome { get; init; }
    public CampoOpcional<decimal> Teto { get; init; }
}

public sealed class ContaFixaCriarRequest
{
    public CampoOpcional<string> Nome { get; init; }
    public CampoOpcional<decimal> Valor { get; init; }
}

public sealed class ContaFixaAtualizarRequest
{
    public CampoOpcional<string> Nome { get; init; }
    public CampoOpcional<decimal> Valor { get; init; }
}

public sealed class RendaAtualizarRequest
{
    public CampoOpcional<decimal> RendaGuilherme { get; init; }
    public CampoOpcional<decimal> RendaEsposa { get; init; }
}
