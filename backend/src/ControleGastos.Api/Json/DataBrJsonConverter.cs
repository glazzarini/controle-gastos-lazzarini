using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ControleGastos.Api.Json;

/// <summary>Datas trafegam como <c>DD/MM/YYYY</c> (contrato atual).</summary>
public sealed class DataBrJsonConverter : JsonConverter<DateOnly>
{
    public const string Formato = "dd/MM/yyyy";

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        DateOnly.TryParseExact(reader.GetString(), Formato, CultureInfo.InvariantCulture, DateTimeStyles.None, out var data)
            ? data
            : throw new JsonException($"Data deve estar no formato {Formato}");

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString(Formato, CultureInfo.InvariantCulture));
}
