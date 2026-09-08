using System.Text.Json;
using System.Text.Json.Serialization;

namespace IvanTest.Common;

/// <summary>
/// DateTime 统一序列化为 yyyy-MM-dd（反序列化兼容 yyyy-MM-dd 与完整时间戳）
/// </summary>
public class DateTimeJsonConverter : JsonConverter<DateTime>
{
    private const string DateFormat = "yyyy-MM-dd";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (DateTime.TryParse(value, out var dt))
        {
            return dt;
        }
        throw new JsonException($"无法解析日期：{value}");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(DateFormat));
    }
}

/// <summary>
/// DateTime? 统一序列化为 yyyy-MM-dd，空值输出 null
/// </summary>
public class NullableDateTimeJsonConverter : JsonConverter<DateTime?>
{
    private const string DateFormat = "yyyy-MM-dd";

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }
        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        if (DateTime.TryParse(value, out var dt))
        {
            return dt;
        }
        throw new JsonException($"无法解析日期：{value}");
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString(DateFormat));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
