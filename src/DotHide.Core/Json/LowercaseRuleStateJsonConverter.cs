using System.Text.Json;
using System.Text.Json.Serialization;
using DotHide.Models;

namespace DotHide.Json;

public sealed class LowercaseRuleStateJsonConverter : JsonConverter<RuleState>
{
    public override RuleState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value?.Trim().ToLowerInvariant() switch
        {
            "hide" => RuleState.Hide,
            "show" => RuleState.Show,
            _ => throw new JsonException($"Unknown rule state '{value}'.")
        };
    }

    public override void Write(Utf8JsonWriter writer, RuleState value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value == RuleState.Hide ? "hide" : "show");
    }
}
