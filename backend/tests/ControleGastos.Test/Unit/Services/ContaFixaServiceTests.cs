using ControleGastos.Core.Dtos;
using ControleGastos.Core.Dtos.Requests;
using ControleGastos.Core.Entities;
using ControleGastos.Core.Exceptions;
using ControleGastos.Core.Interfaces;
using ControleGastos.Core.Services;
using ControleGastos.Test.Fixtures;
using NSubstitute;

namespace ControleGastos.Test.Unit.Services;

public sealed class ContaFixaServiceTests : TesteBase
{
    private readonly IContaFixaRepository _repo = Substitute.For<IContaFixaRepository>();
    private readonly ContaFixaService _service;
    private static readonly ContaFixa Internet = new() { Id = Guid.NewGuid(), Nome = "Internet", Valor = 150, CreatedAt = DateTime.UtcNow };

    public ContaFixaServiceTests() => _service = new ContaFixaService(_repo);

    [Fact]
    public async Task Listar_RetornaContasMapeadas()
    {
        _repo.ListarAsync(Arg.Any<CancellationToken>()).Returns([Internet]);

        var item = Assert.Single(await _service.ListarAsync(Ct));

        Assert.Equal(Internet.Id, item.Id);
        Assert.Equal(150m, item.Valor);
        Assert.Equal(Internet.CreatedAt, item.CreatedAt);
    }

    [Fact]
    public async Task Criar_Valida_AplicaTrim()
    {
        _repo.CriarAsync("Internet", 120, Arg.Any<CancellationToken>()).Returns(Internet);

        var criada = await _service.CriarAsync(new ContaFixaCriarRequest { Nome = Texto("  Internet  "), Valor = Numero(120) }, Ct);

        Assert.Equal("Internet", criada.Nome);
    }

    [Fact]
    public async Task Criar_ValorZero_Aceita()
    {
        _repo.CriarAsync("X", 0, Arg.Any<CancellationToken>()).Returns(Internet);
        await _service.CriarAsync(new ContaFixaCriarRequest { Nome = Texto("X"), Valor = Numero(0) }, Ct);
        await _repo.Received(1).CriarAsync("X", 0, Arg.Any<CancellationToken>());
    }

    public static TheoryData<CampoOpcional<string>> NomesInvalidos => new()
    {
        CampoOpcional.Ausente<string>(), CampoOpcional.Nulo<string>(), CampoOpcional.Vazio<string>(), CampoOpcional.De("  "), CampoOpcional.Invalido<string>(),
    };

    [Theory]
    [MemberData(nameof(NomesInvalidos))]
    public async Task Criar_NomeAusenteOuVazio_Retorna400(CampoOpcional<string> nome)
    {
        var erro = await Assert.ThrowsAsync<ValidacaoException>(
            () => _service.CriarAsync(new ContaFixaCriarRequest { Nome = nome, Valor = Numero(10) }, Ct));
        Assert.Equal(ContaFixaService.MensagemNomeObrigatorio, erro.Message);
    }

    public static TheoryData<CampoOpcional<decimal>> ValoresInvalidos => new()
    {
        CampoOpcional.Ausente<decimal>(), CampoOpcional.Nulo<decimal>(), CampoOpcional.Vazio<decimal>(), CampoOpcional.Invalido<decimal>(), CampoOpcional.De(-1m),
    };

    [Theory]
    [MemberData(nameof(ValoresInvalidos))]
    public async Task Criar_ValorAusenteInvalidoOuNegativo_Retorna400(CampoOpcional<decimal> valor)
    {
        var erro = await Assert.ThrowsAsync<ValidacaoException>(
            () => _service.CriarAsync(new ContaFixaCriarRequest { Nome = Texto("X"), Valor = valor }, Ct));
        Assert.Equal(ContaFixaService.MensagemValor, erro.Message);
    }

    [Fact]
    public async Task Atualizar_NomeEValor()
    {
        _repo.AtualizarAsync("1", "Internet", 150, Arg.Any<CancellationToken>()).Returns(Internet);

        var resultado = await _service.AtualizarAsync(
            "1", new ContaFixaAtualizarRequest { Nome = Texto("Internet"), Valor = Numero(150) }, Ct);

        Assert.True(resultado.Updated);
        Assert.Equal(Internet.Id, resultado.ContaFixa.Id);
    }

    [Fact]
    public async Task Atualizar_SoNome()
    {
        _repo.AtualizarAsync("1", "Novo Nome", null, Arg.Any<CancellationToken>()).Returns(Internet);
        await _service.AtualizarAsync("1", new ContaFixaAtualizarRequest { Nome = Texto("Novo Nome") }, Ct);
        await _repo.Received(1).AtualizarAsync("1", "Novo Nome", null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Atualizar_SoValor()
    {
        _repo.AtualizarAsync("1", null, 200, Arg.Any<CancellationToken>()).Returns(Internet);
        await _service.AtualizarAsync("1", new ContaFixaAtualizarRequest { Valor = Numero(200) }, Ct);
        await _repo.Received(1).AtualizarAsync("1", null, 200, Arg.Any<CancellationToken>());
    }

    public static TheoryData<CampoOpcional<string>> NomesPresentesInvalidos => new()
    {
        CampoOpcional.Nulo<string>(), CampoOpcional.Vazio<string>(), CampoOpcional.De("   "), CampoOpcional.Invalido<string>(),
    };

    [Theory]
    [MemberData(nameof(NomesPresentesInvalidos))]
    public async Task Atualizar_NomeVazio_Retorna400SemIrAoBanco(CampoOpcional<string> nome)
    {
        var erro = await Assert.ThrowsAsync<ValidacaoException>(
            () => _service.AtualizarAsync("1", new ContaFixaAtualizarRequest { Nome = nome }, Ct));
        Assert.Equal(ContaFixaService.MensagemNomeVazio, erro.Message);
        Assert.Empty(_repo.ReceivedCalls());
    }

    public static TheoryData<CampoOpcional<decimal>> ValoresPresentesInvalidos => new()
    {
        CampoOpcional.Nulo<decimal>(), CampoOpcional.Vazio<decimal>(), CampoOpcional.Invalido<decimal>(), CampoOpcional.De(-1m),
    };

    [Theory]
    [MemberData(nameof(ValoresPresentesInvalidos))]
    public async Task Atualizar_ValorInvalido_Retorna400(CampoOpcional<decimal> valor)
    {
        var erro = await Assert.ThrowsAsync<ValidacaoException>(
            () => _service.AtualizarAsync("1", new ContaFixaAtualizarRequest { Valor = valor }, Ct));
        Assert.Equal(ContaFixaService.MensagemValor, erro.Message);
    }

    [Fact]
    public async Task Atualizar_Inexistente_Retorna404()
    {
        _repo.AtualizarAsync("x", null, 10, Arg.Any<CancellationToken>()).Returns((ContaFixa?)null);

        var erro = await Assert.ThrowsAsync<NaoEncontradoException>(
            () => _service.AtualizarAsync("x", new ContaFixaAtualizarRequest { Valor = Numero(10) }, Ct));
        Assert.Equal(ContaFixaService.MensagemNaoEncontrada, erro.Message);
    }

    [Fact]
    public async Task Atualizar_SemCampos_DevolveAtual()
    {
        _repo.ObterPorIdAsync("1", Arg.Any<CancellationToken>()).Returns(Internet);

        var resultado = await _service.AtualizarAsync("1", new ContaFixaAtualizarRequest(), Ct);

        Assert.Equal(Internet.Id, resultado.ContaFixa.Id);
        await _repo.DidNotReceiveWithAnyArgs().AtualizarAsync(default!, default, default, Ct);
    }

    [Fact]
    public async Task Atualizar_SemCamposEInexistente_Retorna404()
    {
        _repo.ObterPorIdAsync("x", Arg.Any<CancellationToken>()).Returns((ContaFixa?)null);
        await Assert.ThrowsAsync<NaoEncontradoException>(
            () => _service.AtualizarAsync("x", new ContaFixaAtualizarRequest(), Ct));
    }

    [Fact]
    public async Task Excluir_ChamaRepositorio()
    {
        var resultado = await _service.ExcluirAsync("1", Ct);

        Assert.True(resultado.Deleted);
        await _repo.Received(1).ExcluirAsync("1", Arg.Any<CancellationToken>());
    }
}
