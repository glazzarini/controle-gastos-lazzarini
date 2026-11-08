using ControleGastos.Core.Dtos;
using ControleGastos.Core.Dtos.Requests;
using ControleGastos.Core.Dtos.Responses;
using ControleGastos.Core.Exceptions;
using ControleGastos.Core.Interfaces;
using ControleGastos.Core.Mappings;

namespace ControleGastos.Core.Services;

public sealed class ContaFixaService(IContaFixaRepository repositorio) : IContaFixaService
{
    public const string MensagemNomeObrigatorio = "nome é obrigatório";
    public const string MensagemNomeVazio = "nome não pode ser vazio";
    public const string MensagemValor = "valor deve ser um número maior ou igual a 0";
    public const string MensagemNaoEncontrada = "Conta fixa não encontrada";

    public async Task<IReadOnlyList<ContaFixaResponse>> ListarAsync(CancellationToken ct) =>
        (await repositorio.ListarAsync(ct)).Select(c => c.ToResponse()).ToList();

    public async Task<ContaFixaResponse> CriarAsync(ContaFixaCriarRequest request, CancellationToken ct)
    {
        var nome = Campos.TextoAparado(request.Nome);
        if (nome.Length == 0)
        {
            throw new ValidacaoException(MensagemNomeObrigatorio);
        }
        var valor = ValorValido(request.Valor);
        return (await repositorio.CriarAsync(nome, valor, ct)).ToResponse();
    }

    public async Task<ContaFixaAtualizadaResponse> AtualizarAsync(
        string id, ContaFixaAtualizarRequest request, CancellationToken ct)
    {
        // Como hoje: valida o body antes de ir ao banco.
        string? nome = null;
        if (request.Nome.Presente)
        {
            nome = Campos.TextoAparado(request.Nome);
            if (nome.Length == 0)
            {
                throw new ValidacaoException(MensagemNomeVazio);
            }
        }

        decimal? valor = request.Valor.Presente ? ValorValido(request.Valor) : null;

        var conta = nome is null && valor is null
            ? await repositorio.ObterPorIdAsync(id, ct)
            : await repositorio.AtualizarAsync(id, nome, valor, ct);

        return conta is null
            ? throw new NaoEncontradoException(MensagemNaoEncontrada)
            : new ContaFixaAtualizadaResponse(true, conta.ToResponse());
    }

    public async Task<DeletedResponse> ExcluirAsync(string id, CancellationToken ct)
    {
        await repositorio.ExcluirAsync(id, ct);
        return new DeletedResponse();
    }

    private static decimal ValorValido(CampoOpcional<decimal> valor) =>
        valor.TemValor && valor.Valor >= 0 ? valor.Valor : throw new ValidacaoException(MensagemValor);
}
