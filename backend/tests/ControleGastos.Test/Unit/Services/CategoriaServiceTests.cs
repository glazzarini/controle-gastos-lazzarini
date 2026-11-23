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

public sealed class CategoriaServiceTests : TesteBase
{
    private readonly ICategoriaRepository _repo = Substitute.For<ICategoriaRepository>();
    private readonly CategoriaService _service;
    private static readonly Categoria Pet = new() { Id = Guid.NewGuid(), Nome = "Pet", Teto = 450, CreatedAt = DateTime.UtcNow };

    public CategoriaServiceTests() => _service = new CategoriaService(_repo);

    [Fact]
    public async Task Listar_RetornaCategoriasMapeadas()
    {
        _repo.ListarAsync(Arg.Any<CancellationToken>()).Returns([Pet]);

        var item = Assert.Single(await _service.ListarAsync(Ct));

        Assert.Equal(Pet.Id, item.Id);
        Assert.Equal("Pet", item.Nome);
        Assert.Equal(450m, item.Teto);
        Assert.Equal(Pet.CreatedAt, item.CreatedAt);
    }

    // ── POST ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Criar_AplicaTrimNoNome()
    {
        _repo.CriarAsync("Nova Cat", 100, Arg.Any<CancellationToken>()).Returns(new Categoria { Nome = "Nova Cat", Teto = 100 });

        var criada = await _service.CriarAsync(new CategoriaCriarRequest { Nome = Texto("  Nova Cat  "), Teto = Numero(100) }, Ct);

        Assert.Equal("Nova Cat", criada.Nome);
        Assert.Equal(100m, criada.Teto);
    }

    public static TheoryData<CampoOpcional<string>> NomesInvalidos => new()
    {
        CampoOpcional.Ausente<string>(), CampoOpcional.Nulo<string>(), CampoOpcional.Vazio<string>(),
        CampoOpcional.De("   "), CampoOpcional.Invalido<string>(),
    };

    [Theory]
    [MemberData(nameof(NomesInvalidos))]
    public async Task Criar_NomeAusenteOuVazio_Retorna400(CampoOpcional<string> nome)
    {
        var erro = await Assert.ThrowsAsync<ValidacaoException>(
            () => _service.CriarAsync(new CategoriaCriarRequest { Nome = nome, Teto = Numero(10) }, Ct));
        Assert.Equal(CategoriaService.MensagemNomeObrigatorio, erro.Message);
    }

    public static TheoryData<CampoOpcional<decimal>> TetosQueViramZero => new()
    {
        CampoOpcional.Ausente<decimal>(), CampoOpcional.Nulo<decimal>(), CampoOpcional.Vazio<decimal>(),
    };

    [Theory]
    [MemberData(nameof(TetosQueViramZero))]
    public async Task Criar_TetoOmitido_UsaZero(CampoOpcional<decimal> teto)
    {
        _repo.CriarAsync("Sem Teto", 0, Arg.Any<CancellationToken>()).Returns(new Categoria { Nome = "Sem Teto" });

        var criada = await _service.CriarAsync(new CategoriaCriarRequest { Nome = Texto("Sem Teto"), Teto = teto }, Ct);

        Assert.Equal(0m, criada.Teto);
        await _repo.Received(1).CriarAsync("Sem Teto", 0, Arg.Any<CancellationToken>());
    }

    public static TheoryData<CampoOpcional<decimal>> TetosInvalidos => new()
    {
        CampoOpcional.Invalido<decimal>(), CampoOpcional.De(-1m), CampoOpcional.De(-0.01m),
    };

    [Theory]
    [MemberData(nameof(TetosInvalidos))]
    public async Task Criar_TetoInvalidoOuNegativo_Retorna400(CampoOpcional<decimal> teto)
    {
        var erro = await Assert.ThrowsAsync<ValidacaoException>(
            () => _service.CriarAsync(new CategoriaCriarRequest { Nome = Texto("X"), Teto = teto }, Ct));
        Assert.Equal(CategoriaService.MensagemTeto, erro.Message);
    }

    [Fact]
    public async Task Criar_TetoZero_Aceita()
    {
        _repo.CriarAsync("X", 0, Arg.Any<CancellationToken>()).Returns(new Categoria { Nome = "X" });
        await _service.CriarAsync(new CategoriaCriarRequest { Nome = Texto("X"), Teto = Numero(0) }, Ct);
        await _repo.Received(1).CriarAsync("X", 0, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Criar_NomeDuplicado_Propaga409()
    {
        _repo.CriarAsync("Pet", 10, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConflitoException("Já existe uma categoria com esse nome"));

        var erro = await Assert.ThrowsAsync<ConflitoException>(
            () => _service.CriarAsync(new CategoriaCriarRequest { Nome = Texto("Pet"), Teto = Numero(10) }, Ct));
        Assert.Equal(409, erro.StatusCode);
    }

    // ── PUT ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Atualizar_Inexistente_Retorna404AntesDeValidar()
    {
        _repo.ObterPorIdAsync("x", Arg.Any<CancellationToken>()).Returns((Categoria?)null);

        var erro = await Assert.ThrowsAsync<NaoEncontradoException>(
            () => _service.AtualizarAsync("x", new CategoriaAtualizarRequest { Teto = Numero(-1) }, Ct));
        Assert.Equal(CategoriaService.MensagemNaoEncontrada, erro.Message);
    }

    [Fact]
    public async Task Atualizar_SoTeto_NaoRodaRenomeacao()
    {
        _repo.ObterPorIdAsync("1", Arg.Any<CancellationToken>()).Returns(Pet);
        _repo.AtualizarAsync("1", null, 500, "Pet", Arg.Any<CancellationToken>())
            .Returns((new Categoria { Id = Pet.Id, Nome = "Pet", Teto = 500 }, 0));

        var resultado = await _service.AtualizarAsync("1", new CategoriaAtualizarRequest { Teto = Numero(500) }, Ct);

        Assert.True(resultado.Updated);
        Assert.Equal(500m, resultado.Categoria.Teto);
        Assert.Equal(0, resultado.LancamentosAtualizados);
    }

    [Fact]
    public async Task Atualizar_Renomeia_PropagaParaLancamentos()
    {
        _repo.ObterPorIdAsync("1", Arg.Any<CancellationToken>()).Returns(Pet);
        _repo.AtualizarAsync("1", "Animais", null, "Pet", Arg.Any<CancellationToken>())
            .Returns((new Categoria { Id = Pet.Id, Nome = "Animais", Teto = 450 }, 2));

        var resultado = await _service.AtualizarAsync("1", new CategoriaAtualizarRequest { Nome = Texto(" Animais ") }, Ct);

        Assert.Equal("Animais", resultado.Categoria.Nome);
        Assert.Equal(2, resultado.LancamentosAtualizados);
    }

    [Fact]
    public async Task Atualizar_NomeETeto_EnviaAmbos()
    {
        _repo.ObterPorIdAsync("1", Arg.Any<CancellationToken>()).Returns(Pet);
        _repo.AtualizarAsync("1", "Animais", 10, "Pet", Arg.Any<CancellationToken>())
            .Returns((new Categoria { Nome = "Animais", Teto = 10 }, 1));

        var resultado = await _service.AtualizarAsync(
            "1", new CategoriaAtualizarRequest { Nome = Texto("Animais"), Teto = Numero(10) }, Ct);

        Assert.Equal(1, resultado.LancamentosAtualizados);
    }

    [Theory]
    [MemberData(nameof(NomesPresentesInvalidos))]
    public async Task Atualizar_NomeVazio_Retorna400(CampoOpcional<string> nome)
    {
        _repo.ObterPorIdAsync("1", Arg.Any<CancellationToken>()).Returns(Pet);

        var erro = await Assert.ThrowsAsync<ValidacaoException>(
            () => _service.AtualizarAsync("1", new CategoriaAtualizarRequest { Nome = nome }, Ct));
        Assert.Equal(CategoriaService.MensagemNomeVazio, erro.Message);
    }

    public static TheoryData<CampoOpcional<string>> NomesPresentesInvalidos => new()
    {
        CampoOpcional.Nulo<string>(), CampoOpcional.Vazio<string>(), CampoOpcional.De("   "), CampoOpcional.Invalido<string>(),
    };

    public static TheoryData<CampoOpcional<decimal>> TetosPresentesInvalidos => new()
    {
        CampoOpcional.Nulo<decimal>(), CampoOpcional.Vazio<decimal>(), CampoOpcional.Invalido<decimal>(), CampoOpcional.De(-1m),
    };

    [Theory]
    [MemberData(nameof(TetosPresentesInvalidos))]
    public async Task Atualizar_TetoInvalido_Retorna400(CampoOpcional<decimal> teto)
    {
        _repo.ObterPorIdAsync("1", Arg.Any<CancellationToken>()).Returns(Pet);

        var erro = await Assert.ThrowsAsync<ValidacaoException>(
            () => _service.AtualizarAsync("1", new CategoriaAtualizarRequest { Teto = teto }, Ct));
        Assert.Equal(CategoriaService.MensagemTeto, erro.Message);
    }

    [Fact]
    public async Task Atualizar_SemCampos_DevolveAtualSemAlterar()
    {
        _repo.ObterPorIdAsync("1", Arg.Any<CancellationToken>()).Returns(Pet);

        var resultado = await _service.AtualizarAsync("1", new CategoriaAtualizarRequest(), Ct);

        Assert.Equal("Pet", resultado.Categoria.Nome);
        Assert.Equal(0, resultado.LancamentosAtualizados);
        await _repo.DidNotReceiveWithAnyArgs().AtualizarAsync(default!, default, default, default!, Ct);
    }

    [Fact]
    public async Task Atualizar_RemovidaDuranteAOperacao_Retorna404()
    {
        _repo.ObterPorIdAsync("1", Arg.Any<CancellationToken>()).Returns(Pet);
        _repo.AtualizarAsync("1", null, 1, "Pet", Arg.Any<CancellationToken>())
            .Returns(((Categoria, int)?)null);

        await Assert.ThrowsAsync<NaoEncontradoException>(
            () => _service.AtualizarAsync("1", new CategoriaAtualizarRequest { Teto = Numero(1) }, Ct));
    }

    [Fact]
    public async Task Atualizar_NomeDuplicado_Propaga409()
    {
        _repo.ObterPorIdAsync("1", Arg.Any<CancellationToken>()).Returns(Pet);
        _repo.AtualizarAsync("1", "Guitarra", null, "Pet", Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConflitoException("Já existe uma categoria com esse nome"));

        await Assert.ThrowsAsync<ConflitoException>(
            () => _service.AtualizarAsync("1", new CategoriaAtualizarRequest { Nome = Texto("Guitarra") }, Ct));
    }

    // ── DELETE ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Excluir_SemLancamentos_Exclui()
    {
        _repo.ObterPorIdAsync("1", Arg.Any<CancellationToken>()).Returns(Pet);
        _repo.ContarLancamentosAsync("Pet", Arg.Any<CancellationToken>()).Returns(0);

        var resultado = await _service.ExcluirAsync("1", Ct);

        Assert.True(resultado.Deleted);
        await _repo.Received(1).ExcluirAsync("1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Excluir_EmUso_Retorna409ComQuantidade()
    {
        _repo.ObterPorIdAsync("1", Arg.Any<CancellationToken>()).Returns(Pet);
        _repo.ContarLancamentosAsync("Pet", Arg.Any<CancellationToken>()).Returns(3);

        var erro = await Assert.ThrowsAsync<ConflitoException>(() => _service.ExcluirAsync("1", Ct));

        Assert.Equal("Não é possível excluir: 3 lançamento(s) usam essa categoria", erro.Message);
        Assert.Equal(3, erro.Extras["lancamentos_em_uso"]);
        await _repo.DidNotReceiveWithAnyArgs().ExcluirAsync(default!, Ct);
    }

    [Fact]
    public async Task Excluir_Inexistente_Retorna404()
    {
        _repo.ObterPorIdAsync("x", Arg.Any<CancellationToken>()).Returns((Categoria?)null);
        await Assert.ThrowsAsync<NaoEncontradoException>(() => _service.ExcluirAsync("x", Ct));
    }
}
