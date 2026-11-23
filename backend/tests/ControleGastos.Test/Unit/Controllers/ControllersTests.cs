using ControleGastos.Api.Controllers;
using ControleGastos.Core.Dtos.Requests;
using ControleGastos.Core.Dtos.Responses;
using ControleGastos.Core.Interfaces;
using ControleGastos.Test.Fixtures;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace ControleGastos.Test.Unit.Controllers;

public sealed class LancamentosControllerTests : TesteBase
{
    private readonly ILancamentoService _service = Substitute.For<ILancamentoService>();
    private readonly LancamentosController _controller;

    public LancamentosControllerTests() => _controller = new LancamentosController(_service);

    [Fact]
    public async Task Listar_Retorna200ComLista()
    {
        IReadOnlyList<LancamentoResponse> lista = [];
        _service.ListarAsync("2026-06", Ct).Returns(lista);

        var resultado = Assert.IsType<OkObjectResult>(await _controller.Listar("2026-06", Ct));

        Assert.Same(lista, resultado.Value);
    }

    [Fact]
    public async Task Criar_Retorna201()
    {
        var request = new LancamentoCriarRequest();
        var criado = new LancamentosCriadosResponse(1, [Guid.NewGuid()]);
        _service.CriarAsync(request, Ct).Returns(criado);

        var resultado = Assert.IsType<ObjectResult>(await _controller.Criar(request, Ct));

        Assert.Equal(201, resultado.StatusCode);
        Assert.Same(criado, resultado.Value);
    }

    [Fact]
    public async Task Criar_SemBody_UsaRequestVazio()
    {
        await _controller.Criar(null, Ct);
        await _service.Received(1).CriarAsync(Arg.Is<LancamentoCriarRequest>(r => !r.Valor.Presente), Ct);
    }

    [Fact]
    public async Task Atualizar_Retorna200()
    {
        var request = new LancamentoAtualizarRequest();
        var atualizado = new LancamentoAtualizadoResponse(true, new LancamentoResponse(Guid.NewGuid(), null, null, "Pet", 1, null, null, null));
        _service.AtualizarAsync("1", request, Ct).Returns(atualizado);

        var resultado = Assert.IsType<OkObjectResult>(await _controller.Atualizar("1", request, Ct));

        Assert.Same(atualizado, resultado.Value);
    }

    [Fact]
    public async Task Atualizar_SemBody_UsaRequestVazio()
    {
        await _controller.Atualizar("1", null, Ct);
        await _service.Received(1).AtualizarAsync("1", Arg.Any<LancamentoAtualizarRequest>(), Ct);
    }

    [Fact]
    public async Task Excluir_Retorna200()
    {
        var excluido = new DeletedResponse();
        _service.ExcluirAsync("1", Ct).Returns(excluido);

        var resultado = Assert.IsType<OkObjectResult>(await _controller.Excluir("1", Ct));

        Assert.Same(excluido, resultado.Value);
    }
}

public sealed class CategoriasControllerTests : TesteBase
{
    private readonly ICategoriaService _service = Substitute.For<ICategoriaService>();
    private readonly CategoriasController _controller;

    public CategoriasControllerTests() => _controller = new CategoriasController(_service);

    [Fact]
    public async Task Listar_Retorna200()
    {
        IReadOnlyList<CategoriaResponse> lista = [];
        _service.ListarAsync(Ct).Returns(lista);
        Assert.Same(lista, Assert.IsType<OkObjectResult>(await _controller.Listar(Ct)).Value);
    }

    [Fact]
    public async Task Criar_Retorna201_ESemBodyUsaRequestVazio()
    {
        var criada = new CategoriaResponse(Guid.NewGuid(), "X", 0, null);
        _service.CriarAsync(Arg.Any<CategoriaCriarRequest>(), Ct).Returns(criada);

        var comBody = Assert.IsType<ObjectResult>(await _controller.Criar(new CategoriaCriarRequest(), Ct));
        var semBody = Assert.IsType<ObjectResult>(await _controller.Criar(null, Ct));

        Assert.Equal(201, comBody.StatusCode);
        Assert.Same(criada, semBody.Value);
        await _service.Received(2).CriarAsync(Arg.Any<CategoriaCriarRequest>(), Ct);
    }

    [Fact]
    public async Task Atualizar_Retorna200_ESemBodyUsaRequestVazio()
    {
        var atualizada = new CategoriaAtualizadaResponse(true, new CategoriaResponse(Guid.NewGuid(), "X", 0, null), 0);
        _service.AtualizarAsync("1", Arg.Any<CategoriaAtualizarRequest>(), Ct).Returns(atualizada);

        Assert.Same(atualizada, Assert.IsType<OkObjectResult>(await _controller.Atualizar("1", new CategoriaAtualizarRequest(), Ct)).Value);
        Assert.Same(atualizada, Assert.IsType<OkObjectResult>(await _controller.Atualizar("1", null, Ct)).Value);
    }

    [Fact]
    public async Task Excluir_Retorna200()
    {
        var excluida = new DeletedResponse();
        _service.ExcluirAsync("1", Ct).Returns(excluida);
        Assert.Same(excluida, Assert.IsType<OkObjectResult>(await _controller.Excluir("1", Ct)).Value);
    }
}

public sealed class ContasFixasControllerTests : TesteBase
{
    private readonly IContaFixaService _service = Substitute.For<IContaFixaService>();
    private readonly ContasFixasController _controller;

    public ContasFixasControllerTests() => _controller = new ContasFixasController(_service);

    [Fact]
    public async Task Listar_Retorna200()
    {
        IReadOnlyList<ContaFixaResponse> lista = [];
        _service.ListarAsync(Ct).Returns(lista);
        Assert.Same(lista, Assert.IsType<OkObjectResult>(await _controller.Listar(Ct)).Value);
    }

    [Fact]
    public async Task Criar_Retorna201_ESemBodyUsaRequestVazio()
    {
        var criada = new ContaFixaResponse(Guid.NewGuid(), "X", 0, null);
        _service.CriarAsync(Arg.Any<ContaFixaCriarRequest>(), Ct).Returns(criada);

        Assert.Equal(201, Assert.IsType<ObjectResult>(await _controller.Criar(new ContaFixaCriarRequest(), Ct)).StatusCode);
        Assert.Same(criada, Assert.IsType<ObjectResult>(await _controller.Criar(null, Ct)).Value);
    }

    [Fact]
    public async Task Atualizar_Retorna200_ESemBodyUsaRequestVazio()
    {
        var atualizada = new ContaFixaAtualizadaResponse(true, new ContaFixaResponse(Guid.NewGuid(), "X", 0, null));
        _service.AtualizarAsync("1", Arg.Any<ContaFixaAtualizarRequest>(), Ct).Returns(atualizada);

        Assert.Same(atualizada, Assert.IsType<OkObjectResult>(await _controller.Atualizar("1", new ContaFixaAtualizarRequest(), Ct)).Value);
        Assert.Same(atualizada, Assert.IsType<OkObjectResult>(await _controller.Atualizar("1", null, Ct)).Value);
    }

    [Fact]
    public async Task Excluir_Retorna200()
    {
        var excluida = new DeletedResponse();
        _service.ExcluirAsync("1", Ct).Returns(excluida);
        Assert.Same(excluida, Assert.IsType<OkObjectResult>(await _controller.Excluir("1", Ct)).Value);
    }
}

public sealed class ConfigControllerTests : TesteBase
{
    private readonly IConfigService _service = Substitute.For<IConfigService>();
    private readonly ConfigController _controller;

    public ConfigControllerTests() => _controller = new ConfigController(_service);

    [Fact]
    public async Task Obter_Retorna200()
    {
        var config = new ConfigResponse(1, 2, 3, 4, new Dictionary<string, decimal>(), []);
        _service.ObterAsync(Ct).Returns(config);
        Assert.Same(config, Assert.IsType<OkObjectResult>(await _controller.Obter(Ct)).Value);
    }

    [Fact]
    public async Task AtualizarRenda_Retorna200_ESemBodyUsaRequestVazio()
    {
        var atualizado = new UpdatedResponse();
        _service.AtualizarRendaAsync(Arg.Any<RendaAtualizarRequest>(), Ct).Returns(atualizado);

        Assert.Same(atualizado, Assert.IsType<OkObjectResult>(await _controller.AtualizarRenda(new RendaAtualizarRequest(), Ct)).Value);
        Assert.Same(atualizado, Assert.IsType<OkObjectResult>(await _controller.AtualizarRenda(null, Ct)).Value);
    }
}
