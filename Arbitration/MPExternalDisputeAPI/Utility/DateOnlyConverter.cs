using Microsoft.VisualBasic;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MPExternalDisputeAPI.Utility
{
    public class DateOnlyConverter : JsonConverter<DateOnly?>
    {
        public override void Write(Utf8JsonWriter writer, DateOnly? date, JsonSerializerOptions options)
        {
            writer.WriteStringValue(date.HasValue ? date.Value.ToString("yyyy-MM-dd") : "");
        }

        public override DateOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dateStr = reader.GetString();
            return !string.IsNullOrEmpty(dateStr) ? DateOnly.Parse(dateStr) : null;
        }
    }
}
