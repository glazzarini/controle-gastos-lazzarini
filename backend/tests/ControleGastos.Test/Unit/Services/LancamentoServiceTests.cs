using ControleGastos.Core.Dtos;
using ControleGastos.Core.Dtos.Requests;
using ControleGastos.Core.Entities;
using ControleGastos.Core.Exceptions;
using ControleGastos.Core.Interfaces;
using ControleGastos.Core.Services;
using ControleGastos.Test.Fixtures;
using NSubstitute;

namespace ControleGastos.Test.Unit.Services;

public sealed class LancamentoServiceTests : TesteBase
{
    private readonly ILancamentoRepository _repo = Substitute.For<ILancamentoRepository>();
    private readonly LancamentoService _service;
    private IReadOnlyList<NovoLancamento> _inseridos = [];

    public LancamentoServiceTests()
    {
        _service = new LancamentoService(_repo);
        _repo.InserirAsync(Arg.Do<IReadOnlyList<NovoLancamento>>(l => _inseridos = l), Arg.Any<CancellationToken>())
            .Returns(c => c.Arg<IReadOnlyList<NovoLancamento>>().Select(_ => Guid.NewGuid()).ToList());
    }

    private static Lancamento Lanc(DateTime? data = null, decimal valor = 10) => new()
    {
        Id = Guid.NewGuid(), Data = data ?? new DateTime(2026, 6, 5), Categoria = "Pet", Valor = valor,
        Descricao = "d", Tipo = "A vista", CreatedAt = DateTime.UtcNow,
    };

    private static LancamentoCriarRequest Valido(
        CampoOpcional<decimal>? valor = null, CampoOpcional<decimal>? parcelas = null,
        string data = "01/06/2026", CampoOpcional<string>? descricao = null) => new()
    {
        Data = CampoOpcional.De(data),
        Categoria = CampoOpcional.De("Pet"),
        Valor = valor ?? CampoOpcional.De(100m),
        Parcelas = parcelas ?? default,
        Descricao = descricao ?? default,
    };

    // ── GET ────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Listar_SemMes_ListaTodos(string? mes)
    {
        _repo.ListarAsync(null, null, Arg.Any<CancellationToken>()).Returns([Lanc()]);

        var resultado = await _service.ListarAsync(mes, Ct);

        Assert.Single(resultado);
        await _repo.Received(1).ListarAsync(null, null, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("2026-06", 2026, 6, 30)]
    [InlineData("2026-2", 2026, 2, 28)]
    [InlineData("2028-02", 2028, 2, 29)]
    [InlineData("2026-12", 2026, 12, 31)]
    public async Task Listar_ComMes_FiltraDoPrimeiroAoUltimoDia(string mes, int ano, int numeroMes, int ultimoDia)
    {
        _repo.ListarAsync(Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>()).Returns([]);

        await _service.ListarAsync(mes, Ct);

        await _repo.Received(1).ListarAsync(
            new DateTime(ano, numeroMes, 1), new DateTime(ano, numeroMes, ultimoDia), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("2026-13")]
    [InlineData("2026-00")]
    [InlineData("0000-05")]
    [InlineData("2026-06-01")]
    [InlineData("06/2026")]
    public async Task Listar_MesInvalido_Retorna400(string mes)
    {
        var erro = await Assert.ThrowsAsync<ValidacaoException>(() => _service.ListarAsync(mes, Ct));
        Assert.Equal(LancamentoService.MensagemMes, erro.Message);
        Assert.Equal(400, erro.StatusCode);
    }

    [Fact]
    public async Task Listar_ConverteEntidadeParaResponse()
    {
        var lanc = Lanc(new DateTime(2026, 6, 5), -50);
        _repo.ListarAsync(null, null, Arg.Any<CancellationToken>()).Returns([lanc]);

        var item = Assert.Single(await _service.ListarAsync(null, Ct));

        Assert.Equal(lanc.Id, item.Id);
        Assert.Equal(new DateOnly(2026, 6, 5), item.Data);
        Assert.Equal(-50m, item.Valor);
    }

    // ── POST: obrigatórios ────────────────────────────────────────────────

    public static TheoryData<LancamentoCriarRequest> RequestsSemObrigatorios => new()
    {
        new LancamentoCriarRequest(),
        new LancamentoCriarRequest { Categoria = CampoOpcional.De("Pet"), Valor = CampoOpcional.De(10m) },
        new LancamentoCriarRequest { Data = CampoOpcional.Vazio<string>(), Categoria = CampoOpcional.De("Pet"), Valor = CampoOpcional.De(10m) },
        new LancamentoCriarRequest { Data = CampoOpcional.Nulo<string>(), Categoria = CampoOpcional.De("Pet"), Valor = CampoOpcional.De(10m) },
        new LancamentoCriarRequest { Data = CampoOpcional.De("01/06/2026"), Valor = CampoOpcional.De(10m) },
        new LancamentoCriarRequest { Data = CampoOpcional.De("01/06/2026"), Categoria = CampoOpcional.Vazio<string>(), Valor = CampoOpcional.De(10m) },
        new LancamentoCriarRequest { Data = CampoOpcional.De("01/06/2026"), Categoria = CampoOpcional.Invalido<string>(), Valor = CampoOpcional.De(10m) },
        new LancamentoCriarRequest { Data = CampoOpcional.De("01/06/2026"), Categoria = CampoOpcional.De("Pet") },
        new LancamentoCriarRequest { Data = CampoOpcional.De("01/06/2026"), Categoria = CampoOpcional.De("Pet"), Valor = CampoOpcional.Nulo<decimal>() },
        new LancamentoCriarRequest { Data = CampoOpcional.De("01/06/2026"), Categoria = CampoOpcional.De("Pet"), Valor = CampoOpcional.Vazio<decimal>() },
        new LancamentoCriarRequest { Data = CampoOpcional.De("01/06/2026"), Categoria = CampoOpcional.De("Pet"), Valor = CampoOpcional.De(0m) },
        new LancamentoCriarRequest { Data = CampoOpcional.De(""), Categoria = CampoOpcional.De("Pet"), Valor = CampoOpcional.De(10m) },
    };

    [Theory]
    [MemberData(nameof(RequestsSemObrigatorios))]
    public async Task Criar_SemCamposObrigatorios_Retorna400(LancamentoCriarRequest request)
    {
        var erro = await Assert.ThrowsAsync<ValidacaoException>(() => _service.CriarAsync(request, Ct));
        Assert.Equal(LancamentoService.MensagemObrigatorios, erro.Message);
        await _repo.DidNotReceiveWithAnyArgs().InserirAsync(default!, Ct);
    }

    [Fact]
    public async Task Criar_ValorNaoNumerico_Retorna400()
    {
        var erro = await Assert.ThrowsAsync<ValidacaoException>(
            () => _service.CriarAsync(Valido(valor: CampoOpcional.Invalido<decimal>()), Ct));
        Assert.Equal(LancamentoService.MensagemValorNumero, erro.Message);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("31/02/2026")]
    [InlineData("2026-06-01")]
    [InlineData("01/13/2026")]
    public async Task Criar_DataInvalida_Retorna400(string data)
    {
        var erro = await Assert.ThrowsAsync<ValidacaoException>(() => _service.CriarAsync(Valido(data: data), Ct));
        Assert.Equal(LancamentoService.MensagemData, erro.Message);
    }

    [Fact]
    public async Task Criar_DataComTipoInvalido_Retorna400DeFormato()
    {
        var request = new LancamentoCriarRequest
        {
            Data = CampoOpcional.Invalido<string>(), Categoria = CampoOpcional.De("Pet"), Valor = CampoOpcional.De(10m),
        };
        var erro = await Assert.ThrowsAsync<ValidacaoException>(() => _service.CriarAsync(request, Ct));
        Assert.Equal(LancamentoService.MensagemData, erro.Message);
    }

    // ── POST: à vista ─────────────────────────────────────────────────────

    [Fact]
    public async Task Criar_AVista_InsereUmaLinhaComTodosOsCampos()
    {
        var request = new LancamentoCriarRequest
        {
            Data = Texto("01/06/2026"), Descricao = Texto("Almoço"), Categoria = Texto("Pet"),
            Valor = Numero(100), Observacao = Texto("obs"), Tipo = Texto("A vista"),
        };

        var resultado = await _service.CriarAsync(request, Ct);

        Assert.Equal(1, resultado.Inserted);
        Assert.Single(resultado.Ids);
        var linha = Assert.Single(_inseridos);
        Assert.Equal(new NovoLancamento(new DateTime(2026, 6, 1), "Almoço", "Pet", 100, "obs", "A vista"), linha);
    }

    [Fact]
    public async Task Criar_SemDescricaoObservacaoETipo_UsaPadroes()
    {
        await _service.CriarAsync(Valido(), Ct);

        var linha = Assert.Single(_inseridos);
        Assert.Equal(string.Empty, linha.Descricao);
        Assert.Null(linha.Observacao);
        Assert.Equal("A vista", linha.Tipo);
    }

    [Fact]
    public async Task Criar_ObservacaoETipoVazios_UsaPadroes()
    {
        var request = new LancamentoCriarRequest
        {
            Data = Texto("01/06/2026"), Categoria = Texto("Pet"), Valor = Numero(10),
            Observacao = Vazio<string>(), Tipo = Vazio<string>(), Descricao = Vazio<string>(),
        };
        await _service.CriarAsync(request, Ct);

        var linha = Assert.Single(_inseridos);
        Assert.Equal(string.Empty, linha.Descricao);
        Assert.Null(linha.Observacao);
        Assert.Equal("A vista", linha.Tipo);
    }

    [Fact]
    public async Task Criar_DataComUmDigito_Aceita()
    {
        await _service.CriarAsync(Valido(data: "5/6/2026"), Ct);
        Assert.Equal(new DateTime(2026, 6, 5), Assert.Single(_inseridos).Data);
    }

    // ── POST: créditos (valor negativo) ──────────────────────────────────

    [Fact]
    public async Task Criar_ValorNegativo_AceitaComoCredito()
    {
        var resultado = await _service.CriarAsync(Valido(valor: Numero(-50)), Ct);

        Assert.Equal(1, resultado.Inserted);
        Assert.Equal(-50m, Assert.Single(_inseridos).Valor);
    }

    [Fact]
    public async Task Criar_CreditoParcelado_DivideValorNegativo()
    {
        await _service.CriarAsync(Valido(valor: Numero(-100), parcelas: Numero(3)), Ct);

        Assert.All(_inseridos, l => Assert.Equal(-33.33m, l.Valor));
    }

    // ── POST: parcelas ────────────────────────────────────────────────────

    [Fact]
    public async Task Criar_Parcelado_DivideValorNumeraDescricaoEAvancaMeses()
    {
        var resultado = await _service.CriarAsync(
            Valido(valor: Numero(300), parcelas: Numero(3), descricao: Texto("Viagem")), Ct);

        Assert.Equal(3, resultado.Inserted);
        Assert.Equal(["Viagem (1/3)", "Viagem (2/3)", "Viagem (3/3)"], _inseridos.Select(l => l.Descricao));
        Assert.Equal(
            [new DateTime(2026, 6, 1), new DateTime(2026, 7, 1), new DateTime(2026, 8, 1)],
            _inseridos.Select(l => l.Data));
        Assert.All(_inseridos, l => Assert.Equal(100m, l.Valor));
    }

    [Fact]
    public async Task Criar_ParceladoArredondaParaDuasCasas()
    {
        await _service.CriarAsync(Valido(valor: Numero(100), parcelas: Numero(3)), Ct);
        Assert.All(_inseridos, l => Assert.Equal(33.33m, l.Valor));
    }

    [Fact]
    public async Task Criar_ParceladoSemDescricao_NumeraSoOSufixo()
    {
        await _service.CriarAsync(Valido(parcelas: Numero(2)), Ct);
        Assert.Equal(["(1/2)", "(2/2)"], _inseridos.Select(l => l.Descricao));
    }

    [Theory]
    [InlineData("31/01/2026", "28/02/2026")]
    [InlineData("31/01/2028", "29/02/2028")]
    [InlineData("30/01/2026", "28/02/2026")]
    [InlineData("31/03/2026", "30/04/2026")]
    public async Task Criar_ParcelaEmMesMaisCurto_UsaUltimoDiaDoMes(string inicio, string segunda)
    {
        await _service.CriarAsync(Valido(parcelas: Numero(2), data: inicio), Ct);
        Assert.Equal(DateTime.ParseExact(segunda, "dd/MM/yyyy", null), _inseridos[1].Data);
    }

    [Fact]
    public async Task Criar_ParcelasDezembroParaJaneiro_ViraOAno()
    {
        await _service.CriarAsync(Valido(parcelas: Numero(2), data: "15/12/2026"), Ct);
        Assert.Equal(new DateTime(2027, 1, 15), _inseridos[1].Data);
    }

    public static TheoryData<CampoOpcional<decimal>, int> CasosDeParcelas => new()
    {
        { default, 1 },
        { CampoOpcional.Nulo<decimal>(), 1 },
        { CampoOpcional.Invalido<decimal>(), 1 },
        { CampoOpcional.De(0m), 1 },
        { CampoOpcional.De(-2m), 1 },
        { CampoOpcional.De(1m), 1 },
        { CampoOpcional.De(3.7m), 3 },
        { CampoOpcional.De(4m), 4 },
        { CampoOpcional.De(1_000_000_000_000m), 1 },
    };

    [Theory]
    [MemberData(nameof(CasosDeParcelas))]
    public async Task Criar_NumeroDeParcelas(CampoOpcional<decimal> parcelas, int esperado)
    {
        var resultado = await _service.CriarAsync(Valido(parcelas: parcelas), Ct);
        Assert.Equal(esperado, resultado.Inserted);
    }

    // ── PUT ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Atualizar_TodosOsCampos_EnviaAlteracoesCompletas()
    {
        LancamentoAlteracoes? recebido = null;
        _repo.AtualizarAsync("1", Arg.Do<LancamentoAlteracoes>(a => recebido = a), Arg.Any<CancellationToken>())
            .Returns(Lanc());
        var request = new LancamentoAtualizarRequest
        {
            Data = Texto("05/06/2026"), Descricao = Texto("Nova"), Categoria = Texto("Pet"),
            Valor = Numero(99), Observacao = Texto("obs"), Tipo = Texto("Parcelado"),
        };

        var resultado = await _service.AtualizarAsync("1", request, Ct);

        Assert.True(resultado.Updated);
        Assert.Equal(new LancamentoAlteracoes
        {
            AlterarData = true, Data = new DateTime(2026, 6, 5),
            AlterarDescricao = true, Descricao = "Nova",
            AlterarCategoria = true, Categoria = "Pet",
            AlterarValor = true, Valor = 99,
            AlterarObservacao = true, Observacao = "obs",
            AlterarTipo = true, Tipo = "Parcelado",
        }, recebido);
    }

    [Fact]
    public async Task Atualizar_DataIso_Aceita()
    {
        LancamentoAlteracoes? recebido = null;
        _repo.AtualizarAsync("1", Arg.Do<LancamentoAlteracoes>(a => recebido = a), Arg.Any<CancellationToken>())
            .Returns(Lanc());

        await _service.AtualizarAsync("1", new LancamentoAtualizarRequest { Data = Texto("2026-06-05") }, Ct);

        Assert.Equal(new DateTime(2026, 6, 5), recebido!.Data);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("2026-02-30")]
    [InlineData("31/02/2026")]
    public async Task Atualizar_DataInvalida_Retorna400(string data)
    {
        var erro = await Assert.ThrowsAsync<ValidacaoException>(
            () => _service.AtualizarAsync("1", new LancamentoAtualizarRequest { Data = Texto(data) }, Ct));
        Assert.Equal(LancamentoService.MensagemData, erro.Message);
    }

    [Fact]
    public async Task Atualizar_DataComTipoInvalido_Retorna400()
    {
        var erro = await Assert.ThrowsAsync<ValidacaoException>(
            () => _service.AtualizarAsync("1", new LancamentoAtualizarRequest { Data = Invalido<string>() }, Ct));
        Assert.Equal(LancamentoService.MensagemData, erro.Message);
    }

    [Fact]
    public async Task Atualizar_CamposVaziosOuNulosQueExigemValor_SaoIgnorados()
    {
        // data/categoria/tipo só são aplicados quando preenchidos (truthy), como hoje.
        var request = new LancamentoAtualizarRequest
        {
            Data = Vazio<string>(), Categoria = Nulo<string>(), Tipo = Vazio<string>(), Valor = Numero(5),
        };
        LancamentoAlteracoes? recebido = null;
        _repo.AtualizarAsync("1", Arg.Do<LancamentoAlteracoes>(a => recebido = a), Arg.Any<CancellationToken>())
            .Returns(Lanc());

        await _service.AtualizarAsync("1", request, Ct);

        Assert.Equal(new LancamentoAlteracoes { AlterarValor = true, Valor = 5 }, recebido);
    }

    public static TheoryData<CampoOpcional<string>, string?> CasosDeTextoOpcional => new()
    {
        { CampoOpcional.Nulo<string>(), null },
        { CampoOpcional.Vazio<string>(), "" },
        { CampoOpcional.De("x"), "x" },
    };

    [Theory]
    [MemberData(nameof(CasosDeTextoOpcional))]
    public async Task Atualizar_DescricaoEObservacao_AceitamNuloEVazio(CampoOpcional<string> campo, string? esperado)
    {
        LancamentoAlteracoes? recebido = null;
        _repo.AtualizarAsync("1", Arg.Do<LancamentoAlteracoes>(a => recebido = a), Arg.Any<CancellationToken>())
            .Returns(Lanc());

        await _service.AtualizarAsync("1", new LancamentoAtualizarRequest { Descricao = campo, Observacao = campo }, Ct);

        Assert.True(recebido!.AlterarDescricao);
        Assert.True(recebido.AlterarObservacao);
        Assert.Equal(esperado, recebido.Descricao);
        Assert.Equal(esperado, recebido.Observacao);
    }

    [Fact]
    public async Task Atualizar_DescricaoComTipoInvalido_Retorna400()
    {
        var erro = await Assert.ThrowsAsync<ValidacaoException>(
            () => _service.AtualizarAsync("1", new LancamentoAtualizarRequest { Descricao = Invalido<string>() }, Ct));
        Assert.Equal("descricao deve ser um texto", erro.Message);
    }

    [Fact]
    public async Task Atualizar_ObservacaoComTipoInvalido_Retorna400()
    {
        var erro = await Assert.ThrowsAsync<ValidacaoException>(
            () => _service.AtualizarAsync("1", new LancamentoAtualizarRequest { Observacao = Invalido<string>() }, Ct));
        Assert.Equal("observacao deve ser um texto", erro.Message);
    }

    public static TheoryData<CampoOpcional<decimal>> ValoresInvalidos => new()
    {
        CampoOpcional.Nulo<decimal>(), CampoOpcional.Vazio<decimal>(), CampoOpcional.Invalido<decimal>(),
    };

    [Theory]
    [MemberData(nameof(ValoresInvalidos))]
    public async Task Atualizar_ValorInvalido_Retorna400(CampoOpcional<decimal> valor)
    {
        var erro = await Assert.ThrowsAsync<ValidacaoException>(
            () => _service.AtualizarAsync("1", new LancamentoAtualizarRequest { Valor = valor }, Ct));
        Assert.Equal(LancamentoService.MensagemValorNumero, erro.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-120.5)]
    public async Task Atualizar_ValorZeroOuNegativo_Aceita(decimal valor)
    {
        LancamentoAlteracoes? recebido = null;
        _repo.AtualizarAsync("1", Arg.Do<LancamentoAlteracoes>(a => recebido = a), Arg.Any<CancellationToken>())
            .Returns(Lanc());

        await _service.AtualizarAsync("1", new LancamentoAtualizarRequest { Valor = Numero(valor) }, Ct);

        Assert.Equal(valor, recebido!.Valor);
    }

    [Fact]
    public async Task Atualizar_Inexistente_Retorna404()
    {
        _repo.AtualizarAsync("x", Arg.Any<LancamentoAlteracoes>(), Arg.Any<CancellationToken>())
            .Returns((Lancamento?)null);

        var erro = await Assert.ThrowsAsync<NaoEncontradoException>(
            () => _service.AtualizarAsync("x", new LancamentoAtualizarRequest { Valor = Numero(1) }, Ct));
        Assert.Equal(LancamentoService.MensagemNaoEncontrado, erro.Message);
        Assert.Equal(404, erro.StatusCode);
    }

    [Fact]
    public async Task Atualizar_SemCampos_DevolveRegistroAtualSemAlterar()
    {
        var atual = Lanc();
        _repo.ObterPorIdAsync("1", Arg.Any<CancellationToken>()).Returns(atual);

        var resultado = await _service.AtualizarAsync("1", new LancamentoAtualizarRequest(), Ct);

        Assert.Equal(atual.Id, resultado.Lancamento.Id);
        await _repo.DidNotReceiveWithAnyArgs().AtualizarAsync(default!, default!, Ct);
    }

    [Fact]
    public async Task Atualizar_SemCamposEInexistente_Retorna404()
    {
        _repo.ObterPorIdAsync("x", Arg.Any<CancellationToken>()).Returns((Lancamento?)null);
        await Assert.ThrowsAsync<NaoEncontradoException>(
            () => _service.AtualizarAsync("x", new LancamentoAtualizarRequest(), Ct));
    }

    // ── DELETE ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Excluir_ChamaRepositorioERetornaDeleted()
    {
        var resultado = await _service.ExcluirAsync("1", Ct);

        Assert.True(resultado.Deleted);
        await _repo.Received(1).ExcluirAsync("1", Arg.Any<CancellationToken>());
    }
}
