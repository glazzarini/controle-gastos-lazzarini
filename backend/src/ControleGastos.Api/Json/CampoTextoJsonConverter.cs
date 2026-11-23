using System.Text.Json;
using System.Text.Json.Serialization;
using ControleGastos.Core.Dtos;

namespace ControleGastos.Api.Json;

/// <summary>Lê um campo texto do body preservando ausente / null / "" / tipo inválido.</summary>
public sealed class CampoTextoJsonConverter : JsonConverter<CampoOpcional<string>>
{
    public override bool HandleNull => true;

    public override CampoOpcional<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return CampoOpcional.Nulo<string>();
            case JsonTokenType.String:
                var texto = reader.GetString()!;
                return texto.Length == 0 ? CampoOpcional.Vazio<string>() : CampoOpcional.De(texto);
            default:
                reader.Skip();
                return CampoOpcional.Invalido<string>();
        }
    }

    public override void Write(Utf8JsonWriter writer, CampoOpcional<string> value, JsonSerializerOptions options)
    {
        switch (value.Estado)
        {
            case EstadoCampo.Valor:
                writer.WriteStringValue(value.Valor);
                break;
            case EstadoCampo.Vazio:
                writer.WriteStringValue(string.Empty);
                break;
            default:
                writer.WriteNullValue();
                break;
        }
    }
}
