using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ControleGastos.Api.Json;

/// <summary>
/// <c>timestamptz</c> no mesmo formato que o PostgREST devolve hoje
/// (<c>2026-08-27T01:19:00.123456+00:00</c>, fração sem zeros à direita).
/// </summary>
public sealed class TimestampJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        DateTime.Parse(reader.GetString()!, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utc = value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        writer.WriteStringValue(utc.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFF", CultureInfo.InvariantCulture) + "+00:00");
    }
}
