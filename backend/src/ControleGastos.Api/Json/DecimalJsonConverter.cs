using System.Text.Json;
using System.Text.Json.Serialization;
using ControleGastos.Core.Services;

namespace ControleGastos.Api.Json;

/// <summary>Valores monetários sempre como <c>number</c>, sem zeros de escala (<c>450.00</c> → <c>450</c>).</summary>
public sealed class DecimalJsonConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetDecimal();

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(Campos.Normalizar(value));
}
