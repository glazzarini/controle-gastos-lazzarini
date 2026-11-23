using System.Text.RegularExpressions;
using ControleGastos.Core.Dtos;
using ControleGastos.Core.Dtos.Requests;
using ControleGastos.Core.Dtos.Responses;
using ControleGastos.Core.Entities;
using ControleGastos.Core.Exceptions;
using ControleGastos.Core.Interfaces;
using ControleGastos.Core.Mappings;

namespace ControleGastos.Core.Services;

public sealed partial class LancamentoService(ILancamentoRepository repositorio) : ILancamentoService
{
    public const string MensagemObrigatorios = "data, categoria e valor são obrigatórios";
    public const string MensagemValorNumero = "valor deve ser um número";
    public const string MensagemData = "data deve estar no formato DD/MM/YYYY";
    public const string MensagemMes = "mes deve estar no formato YYYY-MM";
    public const string MensagemNaoEncontrado = "Lançamento não encontrado";
    public const string TipoPadrao = "A vista";

    [GeneratedRegex(@"^(\d{4})-(\d{1,2})$")]
    private static partial Regex MesRegex();

    public async Task<IReadOnlyList<LancamentoResponse>> ListarAsync(string? mes, CancellationToken ct)
    {
        DateTime? inicio = null;
        DateTime? fim = null;
        if (!string.IsNullOrEmpty(mes))
        {
            var match = MesRegex().Match(mes);
            if (!match.Success)
            {
                throw new ValidacaoException(MensagemMes);
            }
            var ano = int.Parse(match.Groups[1].Value);
            var numeroMes = int.Parse(match.Groups[2].Value);
            if (ano < 1 || numeroMes is < 1 or > 12)
            {
                throw new ValidacaoException(MensagemMes);
            }
            inicio = new DateTime(ano, numeroMes, 1);
            fim = inicio.Value.AddMonths(1).AddDays(-1);
        }

        var lancamentos = await repositorio.ListarAsync(inicio, fim, ct);
        return lancamentos.Select(l => l.ToResponse()).ToList();
    }

    public async Task<LancamentosCriadosResponse> CriarAsync(LancamentoCriarRequest request, CancellationToken ct)
    {
        var dataTruthy = Campos.Preenchido(request.Data) || request.Data.Estado == EstadoCampo.Invalido;
        if (!dataTruthy || !Campos.Preenchido(request.Categoria) || ValorFalsy(request.Valor))
        {
            throw new ValidacaoException(MensagemObrigatorios);
        }
        if (!request.Valor.TemValor)
        {
            throw new ValidacaoException(MensagemValorNumero);
        }
        if (!request.Data.TemValor || !Campos.TryParseDataBr(request.Data.Valor!, out var data))
        {
            throw new ValidacaoException(MensagemData);
        }

        var valor = request.Valor.Valor;
        var parcelas = NumeroDeParcelas(request.Parcelas);
        var valorParcela = parcelas > 1
            ? Math.Round(valor / parcelas, 2, MidpointRounding.AwayFromZero)
            : valor;
        var descricao = request.Descricao.TemValor ? request.Descricao.Valor! : string.Empty;
        var observacao = Campos.Preenchido(request.Observacao) ? request.Observacao.Valor : null;
        var tipo = Campos.Preenchido(request.Tipo) ? request.Tipo.Valor! : TipoPadrao;

        var novos = new List<NovoLancamento>(parcelas);
        for (var i = 0; i < parcelas; i++)
        {
            // Parcela i cai em data + i meses; AddMonths ajusta para o último dia do mês (31/01 → 28/02).
            var descricaoParcela = parcelas > 1 ? $"{descricao} ({i + 1}/{parcelas})".Trim() : descricao;
            novos.Add(new NovoLancamento(
                data.AddMonths(i), descricaoParcela, request.Categoria.Valor!, valorParcela, observacao, tipo));
        }

        var ids = await repositorio.InserirAsync(novos, ct);
        return new LancamentosCriadosResponse(ids.Count, ids);
    }

    public async Task<LancamentoAtualizadoResponse> AtualizarAsync(
        string id, LancamentoAtualizarRequest request, CancellationToken ct)
    {
        var alteracoes = new LancamentoAlteracoes();

        if (Campos.Preenchido(request.Data) || request.Data.Estado == EstadoCampo.Invalido)
        {
            if (!request.Data.TemValor || !Campos.TryParseDataBrOuIso(request.Data.Valor!, out var data))
            {
                throw new ValidacaoException(MensagemData);
            }
            alteracoes = alteracoes with { AlterarData = true, Data = data };
        }
        if (request.Descricao.Presente)
        {
            alteracoes = alteracoes with { AlterarDescricao = true, Descricao = TextoOuNulo(request.Descricao, "descricao") };
        }
        if (Campos.Preenchido(request.Categoria))
        {
            alteracoes = alteracoes with { AlterarCategoria = true, Categoria = request.Categoria.Valor! };
        }
        if (request.Valor.Presente)
        {
            if (!request.Valor.TemValor)
            {
                throw new ValidacaoException(MensagemValorNumero);
            }
            alteracoes = alteracoes with { AlterarValor = true, Valor = request.Valor.Valor };
        }
        if (request.Observacao.Presente)
        {
            alteracoes = alteracoes with { AlterarObservacao = true, Observacao = TextoOuNulo(request.Observacao, "observacao") };
        }
        if (Campos.Preenchido(request.Tipo))
        {
            alteracoes = alteracoes with { AlterarTipo = true, Tipo = request.Tipo.Valor! };
        }

        var lancamento = alteracoes.Vazia
            ? await repositorio.ObterPorIdAsync(id, ct)
            : await repositorio.AtualizarAsync(id, alteracoes, ct);

        return lancamento is null
            ? throw new NaoEncontradoException(MensagemNaoEncontrado)
            : new LancamentoAtualizadoResponse(true, lancamento.ToResponse());
    }

    public async Task<DeletedResponse> ExcluirAsync(string id, CancellationToken ct)
    {
        await repositorio.ExcluirAsync(id, ct);
        return new DeletedResponse();
    }

    /// <summary>Equivale a <c>!valor</c> no JS: ausente, null, "" ou 0.</summary>
    private static bool ValorFalsy(CampoOpcional<decimal> valor) =>
        valor.Estado is EstadoCampo.Ausente or EstadoCampo.Nulo or EstadoCampo.Vazio ||
        (valor.TemValor && valor.Valor == 0);

    /// <summary><c>parseInt(parcelas) || 1</c>, tratando ≤ 0 como 1 (desvio D5).</summary>
    private static int NumeroDeParcelas(CampoOpcional<decimal> parcelas)
    {
        if (!parcelas.TemValor)
        {
            return 1;
        }
        var n = decimal.Truncate(parcelas.Valor);
        return n is >= 1 and <= int.MaxValue ? (int)n : 1;
    }

    private static string? TextoOuNulo(CampoOpcional<string> campo, string nome) => campo.Estado switch
    {
        EstadoCampo.Valor => campo.Valor,
        EstadoCampo.Vazio => string.Empty,
        EstadoCampo.Nulo => null,
        _ => throw new ValidacaoException($"{nome} deve ser um texto"),
    };
}
