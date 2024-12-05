using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Media;

namespace NiVE3.Data.Json.Converter
{
    class ColorJsonConverter : JsonConverter<Color>
    {
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var jsonColor = JsonSerializer.Deserialize(ref reader, typeof(JsonColor), options) as JsonColor;
            if (jsonColor != null)
            {
                return Color.FromArgb(jsonColor.A, jsonColor.R, jsonColor.G, jsonColor.B);
            }
            else
            {
                return Colors.Black;
            }
        }

        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
        {
            var jsonColor = new JsonColor
            {
                R = value.R,
                G = value.G,
                B = value.B,
                A = value.A
            };
            JsonSerializer.Serialize(writer, jsonColor, typeof(JsonColor), options);
        }
    }

    file class JsonColor
    {
        public byte R { get; set; }

        public byte G { get; set; }

        public byte B { get; set; }

        public byte A { get; set; }
    }
}
