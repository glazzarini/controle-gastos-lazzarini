using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using ControleGastos.Core.Dtos;
using ControleGastos.Core.Services;

namespace ControleGastos.Api.Json;

/// <summary>
/// Lê um campo numérico do body aceitando <c>number</c> ou string numérica (<c>"100"</c>, <c>"100.5"</c>).
/// <c>"12abc"</c>, booleanos, objetos e arrays são marcados como inválidos.
/// </summary>
public sealed class CampoNumeroJsonConverter : JsonConverter<CampoOpcional<decimal>>
{
    public override bool HandleNull => true;

    public override CampoOpcional<decimal> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return CampoOpcional.Nulo<decimal>();
            case JsonTokenType.Number:
                return reader.TryGetDecimal(out var numero)
                    ? CampoOpcional.De(numero)
                    : CampoOpcional.Invalido<decimal>();
            case JsonTokenType.String:
                var texto = reader.GetString()!;
                if (texto.Length == 0)
                {
                    return CampoOpcional.Vazio<decimal>();
                }
                return decimal.TryParse(texto, NumberStyles.Float, CultureInfo.InvariantCulture, out var convertido)
                    ? CampoOpcional.De(convertido)
                    : CampoOpcional.Invalido<decimal>();
            default:
                reader.Skip();
                return CampoOpcional.Invalido<decimal>();
        }
    }

    public override void Write(Utf8JsonWriter writer, CampoOpcional<decimal> value, JsonSerializerOptions options)
    {
        if (value.TemValor)
        {
            writer.WriteNumberValue(Campos.Normalizar(value.Valor));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
