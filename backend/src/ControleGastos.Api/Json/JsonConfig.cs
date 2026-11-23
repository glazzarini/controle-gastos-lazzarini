using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ControleGastos.Api.Json;

/// <summary>Serialização do contrato: snake_case, datas DD/MM/YYYY, dinheiro como number, nulls explícitos.</summary>
public static class JsonConfig
{
    public static JsonSerializerOptions Padrao { get; } = Configurar(new JsonSerializerOptions(JsonSerializerDefaults.Web));

    public static JsonSerializerOptions Configurar(JsonSerializerOptions opcoes)
    {
        opcoes.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        opcoes.DictionaryKeyPolicy = null;
        opcoes.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
        opcoes.NumberHandling = JsonNumberHandling.Strict;
        // Igual ao JSON.stringify do Node: acentos literais, sem \uXXXX (a resposta é application/json, não HTML).
        opcoes.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        opcoes.Converters.Add(new CampoTextoJsonConverter());
        opcoes.Converters.Add(new CampoNumeroJsonConverter());
        opcoes.Converters.Add(new DataBrJsonConverter());
        opcoes.Converters.Add(new TimestampJsonConverter());
        opcoes.Converters.Add(new DecimalJsonConverter());
        return opcoes;
    }
}
