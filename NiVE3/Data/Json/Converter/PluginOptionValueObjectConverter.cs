using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiVE3.Data.Json.Converter
{
    public class PluginOptionValueObjectConverter : JsonConverter<object>
    {
        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    return null;
                case JsonTokenType.True:
                    return true;
                case JsonTokenType.False:
                    return false;
                case JsonTokenType.String:
                    return reader.GetString();
                case JsonTokenType.Number:
                    {
                        if (reader.TryGetInt32(out var intValue))
                        {
                            return intValue;
                        }
                        else if (reader.TryGetInt64(out var longValue))
                        {
                            return longValue;
                        }
                        else if (reader.TryGetDouble(out var doubleValue))
                        {
                            return doubleValue;
                        }

                        using var doc = JsonDocument.ParseValue(ref reader);
                        throw new JsonException($"Cannot parse value:{doc.RootElement}");
                    }
                case JsonTokenType.StartArray:
                    {
                        var result = new List<object?>();
                        while (reader.Read())
                        {
                            switch (reader.TokenType)
                            {
                                case JsonTokenType.EndArray:
                                    return result.ToArray();
                                default:
                                    result.Add(Read(ref reader, typeToConvert, options));
                                    break;
                            }
                        }
                        throw new JsonException("Cannot parse invalid array");
                    }
                case JsonTokenType.StartObject:
                    {
                        var result = new Dictionary<string, object?>();
                        while (reader.Read())
                        {
                            switch (reader.TokenType)
                            {
                                case JsonTokenType.EndObject:
                                    return result;
                                case JsonTokenType.PropertyName:
                                    {
                                        var key = reader.GetString();
                                        if (key != null && reader.Read())
                                        {
                                            result.Add(key, Read(ref reader, typeToConvert, options));
                                        }
                                        else
                                        {
                                            throw new JsonException("Cannot parse invalid object");
                                        }
                                    }
                                    break;
                                default:
                                    throw new JsonException("Cannot parse invalid object");
                            }
                        }
                        throw new JsonException("Cannot parse invalid object");
                    }
                default:
                    throw new JsonException($"Unknown token: {reader.TokenType}");
            }
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}
