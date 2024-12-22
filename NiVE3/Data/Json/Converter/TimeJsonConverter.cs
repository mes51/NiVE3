using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Data.Json.Converter
{
    class TimeJsonConverter : JsonConverter<Time>
    {
        public override Time Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Number:
                    {
                        if (reader.TryGetInt32(out var intValue))
                        {
                            return new Time(intValue);
                        }
                        else if (reader.TryGetInt64(out var longValue))
                        {
                            return new Time(longValue);
                        }
                        else if (reader.TryGetDouble(out var doubleValue))
                        {
                            return new Time(doubleValue);
                        }

                        using var doc = JsonDocument.ParseValue(ref reader);
                        throw new JsonException($"Cannot parse value:{doc.RootElement}");
                    }
                case JsonTokenType.StartObject:
                    {
                        var jsonTime = JsonSerializer.Deserialize(ref reader, typeof(JsonFrameTime), options) as JsonFrameTime;
                        if (jsonTime != null)
                        {
                            return new Time(jsonTime.Frame, jsonTime.FrameRate);
                        }

                        using var doc = JsonDocument.ParseValue(ref reader);
                        throw new JsonException($"Cannot parse value:{doc.RootElement}");
                    }
                default:
                    throw new JsonException($"Unknown token: {reader.TokenType}");
            }
        }

        public override void Write(Utf8JsonWriter writer, Time value, JsonSerializerOptions options)
        {
            if (value.IsFrameTime)
            {
                var jsonTime = new JsonFrameTime
                {
                    Frame = value.Frame,
                    FrameRate = value.FrameRate
                };
                JsonSerializer.Serialize(writer, jsonTime, typeof(JsonFrameTime), options);
            }
            else
            {
                writer.WriteNumberValue(value.RealTime);
            }
        }
    }

    file class JsonFrameTime
    {
        public long Frame { get; set; }

        public double FrameRate { get; set; }
    }
}
