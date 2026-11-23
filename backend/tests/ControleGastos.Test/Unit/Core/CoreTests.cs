using ControleGastos.Core.Dtos;
using ControleGastos.Core.Dtos.Responses;
using ControleGastos.Core.Entities;
using ControleGastos.Core.Exceptions;
using ControleGastos.Core.Mappings;
using ControleGastos.Core.Services;

namespace ControleGastos.Test.Unit.Core;

public sealed class CamposTests
{
    [Fact]
    public void Preenchido()
    {
        Assert.True(Campos.Preenchido(CampoOpcional.De("a")));
        Assert.False(Campos.Preenchido(CampoOpcional.De("")));
        Assert.False(Campos.Preenchido(CampoOpcional.Vazio<string>()));
        Assert.False(Campos.Preenchido(CampoOpcional.Nulo<string>()));
        Assert.False(Campos.Preenchido(CampoOpcional.Ausente<string>()));
    }

    [Fact]
    public void TextoAparado()
    {
        Assert.Equal("a b", Campos.TextoAparado(CampoOpcional.De("  a b ")));
        Assert.Equal("", Campos.TextoAparado(CampoOpcional.Invalido<string>()));
    }

    [Theory]
    [InlineData("05/06/2026", true)]
    [InlineData("5/6/2026", true)]
    [InlineData("29/02/2028", true)]
    [InlineData("29/02/2026", false)]
    [InlineData("2026-06-05", false)]
    [InlineData("abc", false)]
    public void TryParseDataBr(string texto, bool valido) => Assert.Equal(valido, Campos.TryParseDataBr(texto, out _));

    [Theory]
    [InlineData("2026-06-05", true)]
    [InlineData("2026-6-5", true)]
    [InlineData("2026-02-30", false)]
    [InlineData("05/06/2026", true)]
    [InlineData("31/06/2026", false)]
    public void TryParseDataBrOuIso(string texto, bool valido) => Assert.Equal(valido, Campos.TryParseDataBrOuIso(texto, out _));

    [Fact]
    public void Normalizar_RemoveZerosDeEscala()
    {
        Assert.Equal("450", Campos.Normalizar(450.00m).ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal("33.3", Campos.Normalizar(33.30m).ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal("21000.5", Campos.ParaTexto(21000.50m));
        Assert.Equal("-50", Campos.ParaTexto(-50.00m));
    }

    [Theory]
    [InlineData("20000", 0, 20000)]
    [InlineData("1.5", 0, 1.5)]
    [InlineData("", 12000, 12000)]
    [InlineData(null, 7, 7)]
    [InlineData("abc", 3, 3)]
    public void NumeroOuPadrao(string? texto, decimal padrao, decimal esperado) =>
        Assert.Equal(esperado, Campos.NumeroOuPadrao(texto, padrao));
}

public sealed class CampoOpcionalTests
{
    [Fact]
    public void Fabricas_DefinemEstado()
    {
        Assert.Equal(EstadoCampo.Ausente, CampoOpcional.Ausente<int>().Estado);
        Assert.Equal(EstadoCampo.Nulo, CampoOpcional.Nulo<int>().Estado);
        Assert.Equal(EstadoCampo.Vazio, CampoOpcional.Vazio<int>().Estado);
        Assert.Equal(EstadoCampo.Invalido, CampoOpcional.Invalido<int>().Estado);
        Assert.Equal(new CampoOpcional<int>(EstadoCampo.Valor, 5), CampoOpcional.De(5));
    }

    [Fact]
    public void PresenteETemValor()
    {
        Assert.False(CampoOpcional.Ausente<int>().Presente);
        Assert.True(CampoOpcional.Nulo<int>().Presente);
        Assert.False(CampoOpcional.Nulo<int>().TemValor);
        Assert.True(CampoOpcional.De(1).TemValor);
    }
}

public sealed class LancamentoAlteracoesTests
{
    [Fact]
    public void Vazia_SoQuandoNenhumCampoMarcado()
    {
        Assert.True(new LancamentoAlteracoes().Vazia);
        Assert.False(new LancamentoAlteracoes { AlterarData = true }.Vazia);
        Assert.False(new LancamentoAlteracoes { AlterarDescricao = true }.Vazia);
        Assert.False(new LancamentoAlteracoes { AlterarCategoria = true }.Vazia);
        Assert.False(new LancamentoAlteracoes { AlterarValor = true }.Vazia);
        Assert.False(new LancamentoAlteracoes { AlterarObservacao = true }.Vazia);
        Assert.False(new LancamentoAlteracoes { AlterarTipo = true }.Vazia);
    }
}

public sealed class MappingsTests
{
    [Fact]
    public void Lancamento_ComData()
    {
        var e = new Lancamento
        {
            Id = Guid.NewGuid(), Data = new DateTime(2026, 6, 5), Descricao = "d", Categoria = "Pet",
            Valor = -10, Observacao = "o", Tipo = "A vista", CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        var r = e.ToResponse();

        Assert.Equal(new LancamentoResponse(e.Id, new DateOnly(2026, 6, 5), "d", "Pet", -10, "o", "A vista", e.CreatedAt), r);
    }

    [Fact]
    public void Lancamento_SemData_MantemNull() => Assert.Null(new Lancamento().ToResponse().Data);

    [Fact]
    public void CategoriaEContaFixa()
    {
        var c = new Categoria { Id = Guid.NewGuid(), Nome = "Pet", Teto = 450, CreatedAt = DateTime.UtcNow };
        Assert.Equal(new CategoriaResponse(c.Id, "Pet", 450, c.CreatedAt), c.ToResponse());

        var cf = new ContaFixa { Id = Guid.NewGuid(), Nome = "Vivo", Valor = 124, CreatedAt = DateTime.UtcNow };
        Assert.Equal(new ContaFixaResponse(cf.Id, "Vivo", 124, cf.CreatedAt), cf.ToResponse());
        Assert.Equal(new ContaFixaResumoResponse(cf.Id, "Vivo", 124), cf.ToResumo());
    }
}

public sealed class ExceptionsTests
{
    [Fact]
    public void StatusCodesEExtras()
    {
        Assert.Equal(400, new ValidacaoException("a").StatusCode);
        Assert.Empty(new ValidacaoException("a").Extras);
        Assert.Equal(404, new NaoEncontradoException("b").StatusCode);
        Assert.Equal(409, new ConflitoException("c").StatusCode);
        Assert.Empty(new ConflitoException("c").Extras);
        Assert.Equal(1, new ConflitoException("c", new Dictionary<string, object> { ["x"] = 1 }).Extras["x"]);
    }

    [Fact]
    public void ResponsesComValoresPadrao()
    {
        Assert.True(new UpdatedResponse().Updated);
        Assert.True(new DeletedResponse().Deleted);
        Assert.Equal("x", new ErroResponse("x").Error);
    }
}
