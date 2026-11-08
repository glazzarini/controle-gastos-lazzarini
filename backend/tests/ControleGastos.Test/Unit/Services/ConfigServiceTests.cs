using ControleGastos.Core.Dtos;
using ControleGastos.Core.Dtos.Requests;
using ControleGastos.Core.Entities;
using ControleGastos.Core.Exceptions;
using ControleGastos.Core.Interfaces;
using ControleGastos.Core.Services;
using ControleGastos.Test.Fixtures;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace ControleGastos.Test.Unit.Services;

public sealed class ConfigServiceTests : TesteBase
{
    private readonly IConfigRepository _config = Substitute.For<IConfigRepository>();
    private readonly ICategoriaRepository _categorias = Substitute.For<ICategoriaRepository>();
    private readonly IContaFixaRepository _contas = Substitute.For<IContaFixaRepository>();
    private readonly ConfigService _service;
    private IReadOnlyList<ConfigItem> _salvos = [];

    public ConfigServiceTests()
    {
        _service = new ConfigService(_config, _categorias, _contas);
        _config.SalvarAsync(Arg.Do<IReadOnlyList<ConfigItem>>(i => _salvos = i), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
    }

    private static ConfigItem Item(string chave, string valor) => new() { Chave = chave, Valor = valor };

    [Fact]
    public async Task Obter_AgregaConfigTetosEContasFixas()
    {
        var condominio = new ContaFixa { Id = Guid.NewGuid(), Nome = "Condomínio", Valor = 1600 };
        _config.ListarAsync(Arg.Any<CancellationToken>()).Returns([
            Item("renda_guilherme", "20000"), Item("renda_esposa", "14000"), Item("meta_fatura", "12000"),
        ]);
        _categorias.ListarAsync(Arg.Any<CancellationToken>()).Returns([new Categoria { Nome = "Pet", Teto = 450 }]);
        _contas.ListarAsync(Arg.Any<CancellationToken>()).Returns([condominio]);

        var config = await _service.ObterAsync(Ct);

        Assert.Equal(20000m, config.RendaGuilherme);
        Assert.Equal(14000m, config.RendaEsposa);
        Assert.Equal(12000m, config.MetaFatura);
        Assert.Equal(1600m, config.TotalFixos);
        Assert.Equal(new Dictionary<string, decimal> { ["Pet"] = 450 }, config.Tetos);
        var conta = Assert.Single(config.ContasFixas);
        Assert.Equal((condominio.Id, "Condomínio", 1600m), (conta.Id, conta.Nome, conta.Valor));
    }

    [Fact]
    public async Task Obter_TabelasVazias_AplicaPadroes()
    {
        _config.ListarAsync(Arg.Any<CancellationToken>()).Returns([]);
        _categorias.ListarAsync(Arg.Any<CancellationToken>()).Returns([]);
        _contas.ListarAsync(Arg.Any<CancellationToken>()).Returns([]);

        var config = await _service.ObterAsync(Ct);

        Assert.Equal(0m, config.RendaGuilherme);
        Assert.Equal(0m, config.RendaEsposa);
        Assert.Equal(12000m, config.MetaFatura);
        Assert.Equal(0m, config.TotalFixos);
        Assert.Empty(config.Tetos);
        Assert.Empty(config.ContasFixas);
    }

    [Fact]
    public async Task Obter_ValoresVaziosOuInvalidosNoBanco_AplicaPadroes()
    {
        _config.ListarAsync(Arg.Any<CancellationToken>()).Returns([
            Item("renda_guilherme", ""), Item("renda_esposa", "abc"), Item("meta_fatura", ""),
        ]);
        _categorias.ListarAsync(Arg.Any<CancellationToken>()).Returns([]);
        _contas.ListarAsync(Arg.Any<CancellationToken>()).Returns([]);

        var config = await _service.ObterAsync(Ct);

        Assert.Equal(0m, config.RendaGuilherme);
        Assert.Equal(0m, config.RendaEsposa);
        Assert.Equal(12000m, config.MetaFatura);
    }

    [Fact]
    public async Task Obter_MetaFaturaZero_Respeita()
    {
        _config.ListarAsync(Arg.Any<CancellationToken>()).Returns([Item("meta_fatura", "0")]);
        _categorias.ListarAsync(Arg.Any<CancellationToken>()).Returns([]);
        _contas.ListarAsync(Arg.Any<CancellationToken>()).Returns([]);

        Assert.Equal(0m, (await _service.ObterAsync(Ct)).MetaFatura);
    }

    [Fact]
    public async Task Obter_TotalFixos_SomaExataComCentavos()
    {
        _config.ListarAsync(Arg.Any<CancellationToken>()).Returns([]);
        _categorias.ListarAsync(Arg.Any<CancellationToken>()).Returns([]);
        _contas.ListarAsync(Arg.Any<CancellationToken>()).Returns([
            new ContaFixa { Nome = "a", Valor = 0.1m }, new ContaFixa { Nome = "b", Valor = 0.2m },
            new ContaFixa { Nome = "c", Valor = 7500 }, new ContaFixa { Nome = "d", Valor = 2300 },
            new ContaFixa { Nome = "e", Valor = 2000 }, new ContaFixa { Nome = "f", Valor = 1600 },
            new ContaFixa { Nome = "g", Valor = 510 }, new ContaFixa { Nome = "h", Valor = 281 },
            new ContaFixa { Nome = "i", Valor = 124 }, new ContaFixa { Nome = "j", Valor = 244 },
        ]);

        var config = await _service.ObterAsync(Ct);

        Assert.Equal(14559.3m, config.TotalFixos);
        Assert.Equal(10, config.ContasFixas.Count);
    }

    [Fact]
    public async Task Obter_TetosMantemOrdemDoRepositorio()
    {
        _config.ListarAsync(Arg.Any<CancellationToken>()).Returns([]);
        _categorias.ListarAsync(Arg.Any<CancellationToken>()).Returns([
            new Categoria { Nome = "Alimentacao fora", Teto = 2000 }, new Categoria { Nome = "Guitarra", Teto = 300 },
            new Categoria { Nome = "Pet", Teto = 0 },
        ]);
        _contas.ListarAsync(Arg.Any<CancellationToken>()).Returns([]);

        var config = await _service.ObterAsync(Ct);

        Assert.Equal(["Alimentacao fora", "Guitarra", "Pet"], config.Tetos.Keys);
        Assert.Equal(0m, config.Tetos["Pet"]);
    }

    [Fact]
    public async Task Obter_ChaveDuplicada_UsaUltima()
    {
        _config.ListarAsync(Arg.Any<CancellationToken>()).Returns([Item("renda_guilherme", "1"), Item("renda_guilherme", "2")]);
        _categorias.ListarAsync(Arg.Any<CancellationToken>()).Returns([]);
        _contas.ListarAsync(Arg.Any<CancellationToken>()).Returns([]);

        Assert.Equal(2m, (await _service.ObterAsync(Ct)).RendaGuilherme);
    }

    [Fact]
    public async Task Obter_ErroNoBanco_Propaga()
    {
        _config.ListarAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("falha config"));
        var erro = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ObterAsync(Ct));
        Assert.Equal("falha config", erro.Message);
    }

    // ── PUT ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task AtualizarRenda_Ambas()
    {
        var resultado = await _service.AtualizarRendaAsync(
            new RendaAtualizarRequest { RendaGuilherme = Numero(21000), RendaEsposa = Numero(15000.50m) }, Ct);

        Assert.True(resultado.Updated);
        Assert.Equal(
            [("renda_guilherme", "21000"), ("renda_esposa", "15000.5")],
            _salvos.Select(i => (i.Chave, i.Valor)));
    }

    [Fact]
    public async Task AtualizarRenda_SoGuilherme()
    {
        await _service.AtualizarRendaAsync(new RendaAtualizarRequest { RendaGuilherme = Numero(21000) }, Ct);
        Assert.Equal("renda_guilherme", Assert.Single(_salvos).Chave);
    }

    [Fact]
    public async Task AtualizarRenda_SoEsposa_AceitaZero()
    {
        await _service.AtualizarRendaAsync(new RendaAtualizarRequest { RendaEsposa = Numero(0) }, Ct);
        var item = Assert.Single(_salvos);
        Assert.Equal(("renda_esposa", "0"), (item.Chave, item.Valor));
    }

    public static TheoryData<CampoOpcional<decimal>> RendasInvalidas => new()
    {
        CampoOpcional.Nulo<decimal>(), CampoOpcional.Vazio<decimal>(), CampoOpcional.Invalido<decimal>(), CampoOpcional.De(-5m),
    };

    [Theory]
    [MemberData(nameof(RendasInvalidas))]
    public async Task AtualizarRenda_GuilhermeInvalida_Retorna400(CampoOpcional<decimal> renda)
    {
        var erro = await Assert.ThrowsAsync<ValidacaoException>(
            () => _service.AtualizarRendaAsync(new RendaAtualizarRequest { RendaGuilherme = renda }, Ct));
        Assert.Equal("renda_guilherme deve ser um número maior ou igual a 0", erro.Message);
    }

    [Theory]
    [MemberData(nameof(RendasInvalidas))]
    public async Task AtualizarRenda_EsposaInvalida_Retorna400(CampoOpcional<decimal> renda)
    {
        var erro = await Assert.ThrowsAsync<ValidacaoException>(
            () => _service.AtualizarRendaAsync(new RendaAtualizarRequest { RendaEsposa = renda }, Ct));
        Assert.Equal("renda_esposa deve ser um número maior ou igual a 0", erro.Message);
    }

    [Fact]
    public async Task AtualizarRenda_SemCampos_Retorna400()
    {
        var erro = await Assert.ThrowsAsync<ValidacaoException>(
            () => _service.AtualizarRendaAsync(new RendaAtualizarRequest(), Ct));
        Assert.Equal(ConfigService.MensagemInforme, erro.Message);
        await _config.DidNotReceiveWithAnyArgs().SalvarAsync(default!, Ct);
    }
}
