using System.Text.Json;
using ControleGastos.Api.Json;
using ControleGastos.Core.Dtos;
using ControleGastos.Core.Dtos.Responses;

namespace ControleGastos.Test.Unit.Json;

public sealed class CampoTextoJsonConverterTests
{
    private sealed class Alvo
    {
        public CampoOpcional<string> Campo { get; init; }
        public CampoOpcional<string> Depois { get; init; }
    }

    private static Alvo Ler(string json) => JsonSerializer.Deserialize<Alvo>(json, JsonConfig.Padrao)!;

    [Theory]
    [InlineData("""{}""", EstadoCampo.Ausente, null)]
    [InlineData("""{"campo":null}""", EstadoCampo.Nulo, null)]
    [InlineData("""{"campo":""}""", EstadoCampo.Vazio, null)]
    [InlineData("""{"campo":"  abc "}""", EstadoCampo.Valor, "  abc ")]
    [InlineData("""{"campo":5}""", EstadoCampo.Invalido, null)]
    [InlineData("""{"campo":true}""", EstadoCampo.Invalido, null)]
    [InlineData("""{"campo":{"a":[1,2]}}""", EstadoCampo.Invalido, null)]
    [InlineData("""{"campo":["x"]}""", EstadoCampo.Invalido, null)]
    public void Read(string json, EstadoCampo estado, string? valor)
    {
        var alvo = Ler(json.Replace("}", ",\"depois\":\"ok\"}", StringComparison.Ordinal).Replace("{,", "{", StringComparison.Ordinal));

        Assert.Equal(estado, alvo.Campo.Estado);
        Assert.Equal(valor, alvo.Campo.Valor);
        Assert.Equal("ok", alvo.Depois.Valor);
    }

    [Fact]
    public void Write()
    {
        var json = JsonSerializer.Serialize(new[]
        {
            CampoOpcional.De("a"), CampoOpcional.Vazio<string>(), CampoOpcional.Nulo<string>(),
            CampoOpcional.Ausente<string>(), CampoOpcional.Invalido<string>(),
        }, JsonConfig.Padrao);

        Assert.Equal("""["a","",null,null,null]""", json);
    }
}

public sealed class CampoNumeroJsonConverterTests
{
    private sealed class Alvo
    {
        public CampoOpcional<decimal> Campo { get; init; }
        public CampoOpcional<string> Depois { get; init; }
    }

    [Theory]
    [InlineData("""{"depois":"ok"}""", EstadoCampo.Ausente, 0)]
    [InlineData("""{"campo":null,"depois":"ok"}""", EstadoCampo.Nulo, 0)]
    [InlineData("""{"campo":"","depois":"ok"}""", EstadoCampo.Vazio, 0)]
    [InlineData("""{"campo":100,"depois":"ok"}""", EstadoCampo.Valor, 100)]
    [InlineData("""{"campo":-50.25,"depois":"ok"}""", EstadoCampo.Valor, -50.25)]
    [InlineData("""{"campo":"100.5","depois":"ok"}""", EstadoCampo.Valor, 100.5)]
    [InlineData("""{"campo":" 12 ","depois":"ok"}""", EstadoCampo.Valor, 12)]
    [InlineData("""{"campo":"1e3","depois":"ok"}""", EstadoCampo.Valor, 1000)]
    [InlineData("""{"campo":"12abc","depois":"ok"}""", EstadoCampo.Invalido, 0)]
    [InlineData("""{"campo":"abc","depois":"ok"}""", EstadoCampo.Invalido, 0)]
    [InlineData("""{"campo":"   ","depois":"ok"}""", EstadoCampo.Invalido, 0)]
    [InlineData("""{"campo":1e400,"depois":"ok"}""", EstadoCampo.Invalido, 0)]
    [InlineData("""{"campo":true,"depois":"ok"}""", EstadoCampo.Invalido, 0)]
    [InlineData("""{"campo":{"x":1},"depois":"ok"}""", EstadoCampo.Invalido, 0)]
    [InlineData("""{"campo":[1],"depois":"ok"}""", EstadoCampo.Invalido, 0)]
    public void Read(string json, EstadoCampo estado, decimal valor)
    {
        var alvo = JsonSerializer.Deserialize<Alvo>(json, JsonConfig.Padrao)!;

        Assert.Equal(estado, alvo.Campo.Estado);
        Assert.Equal(valor, alvo.Campo.Valor);
        Assert.Equal("ok", alvo.Depois.Valor);
    }

    [Fact]
    public void Write()
    {
        var json = JsonSerializer.Serialize(new[]
        {
            CampoOpcional.De(450.00m), CampoOpcional.Nulo<decimal>(), CampoOpcional.Invalido<decimal>(),
        }, JsonConfig.Padrao);

        Assert.Equal("[450,null,null]", json);
    }
}

public sealed class DataBrJsonConverterTests
{
    [Fact]
    public void WriteERead()
    {
        Assert.Equal("\"05/06/2026\"", JsonSerializer.Serialize(new DateOnly(2026, 6, 5), JsonConfig.Padrao));
        Assert.Equal(new DateOnly(2026, 6, 5), JsonSerializer.Deserialize<DateOnly>("\"05/06/2026\"", JsonConfig.Padrao));
        Assert.Equal("null", JsonSerializer.Serialize<DateOnly?>(null, JsonConfig.Padrao));
    }

    [Theory]
    [InlineData("\"2026-06-05\"")]
    [InlineData("\"31/02/2026\"")]
    public void Read_FormatoInvalido_Lanca(string json) =>
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DateOnly>(json, JsonConfig.Padrao));
}

public sealed class TimestampJsonConverterTests
{
    [Fact]
    public void Write_FormatoDoPostgrest()
    {
        var comFracao = new DateTime(2026, 8, 27, 1, 19, 0, DateTimeKind.Utc).AddTicks(1234560);
        var semFracao = new DateTime(2026, 8, 27, 1, 19, 0, DateTimeKind.Utc);
        var naoEspecificado = new DateTime(2026, 8, 27, 1, 19, 0, DateTimeKind.Unspecified);

        Assert.Equal("\"2026-08-27T01:19:00.123456+00:00\"", JsonSerializer.Serialize(comFracao, JsonConfig.Padrao));
        Assert.Equal("\"2026-08-27T01:19:00+00:00\"", JsonSerializer.Serialize(semFracao, JsonConfig.Padrao));
        Assert.Equal("\"2026-08-27T01:19:00+00:00\"", JsonSerializer.Serialize(naoEspecificado, JsonConfig.Padrao));
    }

    [Fact]
    public void Write_HorarioLocal_ConverteParaUtc()
    {
        var utc = new DateTime(2026, 8, 27, 1, 19, 0, DateTimeKind.Utc);
        Assert.Equal("\"2026-08-27T01:19:00+00:00\"", JsonSerializer.Serialize(utc.ToLocalTime(), JsonConfig.Padrao));
    }

    [Fact]
    public void Read_ConverteParaUtc()
    {
        var lido = JsonSerializer.Deserialize<DateTime>("\"2026-08-27T01:19:00.5-03:00\"", JsonConfig.Padrao);
        Assert.Equal(new DateTime(2026, 8, 27, 4, 19, 0, 500, DateTimeKind.Utc), lido);
        Assert.Equal(DateTimeKind.Utc, lido.Kind);
    }
}

public sealed class DecimalJsonConverterTests
{
    [Fact]
    public void WriteComoNumeroSemEscalaERead()
    {
        Assert.Equal("450", JsonSerializer.Serialize(450.00m, JsonConfig.Padrao));
        Assert.Equal("-33.33", JsonSerializer.Serialize(-33.330m, JsonConfig.Padrao));
        Assert.Equal(12.5m, JsonSerializer.Deserialize<decimal>("12.5", JsonConfig.Padrao));
    }
}

public sealed class JsonConfigTests
{
    [Fact]
    public void SnakeCase_NullsExplicitos_ChavesDeDicionarioPreservadas_AcentosLiterais()
    {
        var config = new ConfigResponse(20000, 14000, 12000, 1600,
            new Dictionary<string, decimal> { ["Alimentacao fora"] = 2000 },
            [new ContaFixaResumoResponse(Guid.Empty, "Condomínio", 1600.00m)]);

        var json = JsonSerializer.Serialize(config, JsonConfig.Padrao);

        Assert.Equal(
            """{"renda_guilherme":20000,"renda_esposa":14000,"meta_fatura":12000,"total_fixos":1600,"tetos":{"Alimentacao fora":2000},"contas_fixas":[{"id":"00000000-0000-0000-0000-000000000000","nome":"Condomínio","valor":1600}]}""",
            json);
        Assert.Equal("""{"descricao":null}""", JsonSerializer.Serialize(new { Descricao = (string?)null }, JsonConfig.Padrao));
    }
}

public sealed class LancamentoSemDataJsonTests
{
    // No banco real `data` é NOT NULL; o comportamento do mapeamento/serialização é garantido aqui.
    [Fact]
    [Trait("Vitest", "lancamentos.test.js › GET mantém data como null quando a linha não tem data")]
    [Trait("Vitest", "lancamentos-id.test.js › PUT mantém data como null quando a linha atualizada não tem data")]
    public void LancamentoSemData_SerializaDataNull()
    {
        var response = ControleGastos.Core.Mappings.Mappings.ToResponse(
            new ControleGastos.Core.Entities.Lancamento { Categoria = "Pet", Valor = 10 });

        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(response, JsonConfig.Padrao));

        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("data").ValueKind);
    }
}
