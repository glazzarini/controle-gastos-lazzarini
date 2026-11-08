using ControleGastos.Core.Dtos;
using ControleGastos.Core.Dtos.Requests;
using ControleGastos.Core.Dtos.Responses;
using ControleGastos.Core.Exceptions;
using ControleGastos.Core.Interfaces;
using ControleGastos.Core.Mappings;

namespace ControleGastos.Core.Services;

public sealed class CategoriaService(ICategoriaRepository repositorio) : ICategoriaService
{
    public const string MensagemNomeObrigatorio = "nome é obrigatório";
    public const string MensagemNomeVazio = "nome não pode ser vazio";
    public const string MensagemTeto = "teto deve ser um número maior ou igual a 0";
    public const string MensagemNaoEncontrada = "Categoria não encontrada";

    public async Task<IReadOnlyList<CategoriaResponse>> ListarAsync(CancellationToken ct) =>
        (await repositorio.ListarAsync(ct)).Select(c => c.ToResponse()).ToList();

    public async Task<CategoriaResponse> CriarAsync(CategoriaCriarRequest request, CancellationToken ct)
    {
        var nome = Campos.TextoAparado(request.Nome);
        if (nome.Length == 0)
        {
            throw new ValidacaoException(MensagemNomeObrigatorio);
        }

        // Teto ausente, null ou "" vale 0 (regra atual).
        decimal teto = 0;
        if (request.Teto.Estado is not (EstadoCampo.Ausente or EstadoCampo.Nulo or EstadoCampo.Vazio))
        {
            teto = TetoValido(request.Teto);
        }

        return (await repositorio.CriarAsync(nome, teto, ct)).ToResponse();
    }

    public async Task<CategoriaAtualizadaResponse> AtualizarAsync(
        string id, CategoriaAtualizarRequest request, CancellationToken ct)
    {
        // Como hoje: primeiro verifica se existe, depois valida o body.
        var atual = await repositorio.ObterPorIdAsync(id, ct)
                    ?? throw new NaoEncontradoException(MensagemNaoEncontrada);

        string? nome = null;
        if (request.Nome.Presente)
        {
            nome = Campos.TextoAparado(request.Nome);
            if (nome.Length == 0)
            {
                throw new ValidacaoException(MensagemNomeVazio);
            }
        }

        decimal? teto = request.Teto.Presente ? TetoValido(request.Teto) : null;

        if (nome is null && teto is null)
        {
            return new CategoriaAtualizadaResponse(true, atual.ToResponse(), 0);
        }

        var resultado = await repositorio.AtualizarAsync(id, nome, teto, atual.Nome, ct)
                        ?? throw new NaoEncontradoException(MensagemNaoEncontrada);

        return new CategoriaAtualizadaResponse(true, resultado.Categoria.ToResponse(), resultado.LancamentosAtualizados);
    }

    public async Task<DeletedResponse> ExcluirAsync(string id, CancellationToken ct)
    {
        var atual = await repositorio.ObterPorIdAsync(id, ct)
                    ?? throw new NaoEncontradoException(MensagemNaoEncontrada);

        var emUso = await repositorio.ContarLancamentosAsync(atual.Nome, ct);
        if (emUso > 0)
        {
            throw new ConflitoException(
                $"Não é possível excluir: {emUso} lançamento(s) usam essa categoria",
                new Dictionary<string, object> { ["lancamentos_em_uso"] = emUso });
        }

        await repositorio.ExcluirAsync(id, ct);
        return new DeletedResponse();
    }

    private static decimal TetoValido(CampoOpcional<decimal> teto) =>
        teto.TemValor && teto.Valor >= 0 ? teto.Valor : throw new ValidacaoException(MensagemTeto);
}
