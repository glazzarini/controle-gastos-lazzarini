using ControleGastos.Core.Dtos;
using ControleGastos.Core.Dtos.Requests;
using ControleGastos.Core.Dtos.Responses;
using ControleGastos.Core.Entities;
using ControleGastos.Core.Exceptions;
using ControleGastos.Core.Interfaces;
using ControleGastos.Core.Mappings;

namespace ControleGastos.Core.Services;

public sealed class ConfigService(
    IConfigRepository configRepositorio,
    ICategoriaRepository categoriaRepositorio,
    IContaFixaRepository contaFixaRepositorio) : IConfigService
{
    public const string ChaveRendaGuilherme = "renda_guilherme";
    public const string ChaveRendaEsposa = "renda_esposa";
    public const string ChaveMetaFatura = "meta_fatura";
    public static readonly decimal MetaFaturaPadrao = 12000m;
    public const string MensagemInforme = "Informe renda_guilherme e/ou renda_esposa";

    public async Task<ConfigResponse> ObterAsync(CancellationToken ct)
    {
        var itens = await configRepositorio.ListarAsync(ct);
        var categorias = await categoriaRepositorio.ListarAsync(ct);
        var contasFixas = await contaFixaRepositorio.ListarAsync(ct);

        var mapa = new Dictionary<string, string>();
        foreach (var item in itens)
        {
            mapa[item.Chave] = item.Valor;
        }

        var tetos = new Dictionary<string, decimal>();
        foreach (var categoria in categorias)
        {
            tetos[categoria.Nome] = categoria.Teto;
        }

        var resumoContas = contasFixas.Select(c => c.ToResumo()).ToList();

        return new ConfigResponse(
            RendaGuilherme: Campos.NumeroOuPadrao(mapa.GetValueOrDefault(ChaveRendaGuilherme), 0),
            RendaEsposa: Campos.NumeroOuPadrao(mapa.GetValueOrDefault(ChaveRendaEsposa), 0),
            MetaFatura: Campos.NumeroOuPadrao(mapa.GetValueOrDefault(ChaveMetaFatura), MetaFaturaPadrao),
            TotalFixos: resumoContas.Sum(c => c.Valor),
            Tetos: tetos,
            ContasFixas: resumoContas);
    }

    public async Task<UpdatedResponse> AtualizarRendaAsync(RendaAtualizarRequest request, CancellationToken ct)
    {
        var itens = new List<ConfigItem>();
        AdicionarRenda(itens, ChaveRendaGuilherme, request.RendaGuilherme);
        AdicionarRenda(itens, ChaveRendaEsposa, request.RendaEsposa);

        if (itens.Count == 0)
        {
            throw new ValidacaoException(MensagemInforme);
        }

        await configRepositorio.SalvarAsync(itens, ct);
        return new UpdatedResponse();
    }

    private static void AdicionarRenda(List<ConfigItem> itens, string chave, CampoOpcional<decimal> campo)
    {
        if (!campo.Presente)
        {
            return;
        }
        if (!campo.TemValor || campo.Valor < 0)
        {
            throw new ValidacaoException($"{chave} deve ser um número maior ou igual a 0");
        }
        itens.Add(new ConfigItem { Chave = chave, Valor = Campos.ParaTexto(campo.Valor) });
    }
}
