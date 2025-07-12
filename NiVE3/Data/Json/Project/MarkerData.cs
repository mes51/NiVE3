using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NiVE3.Data.Json.Converter;
using NiVE3.Plugin.ValueObject;

namespace NiVE3.Data.Json.Project
{
    public class MarkerData
    {
        public Guid MarkerId { get; set; }

        [JsonConverter(typeof(TimeJsonConverter))]
        public Time Time { get; set; }

        public string Name { get; set; } = "";
    }
}
