using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SciTrader.Model
{
    internal class JsonDateTimeFmtConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Convert DateTime to microsecond timestamp (if needed)
            if (value is DateTime dateTime)
            {
                long microseconds = new DateTimeOffset(dateTime).ToUnixTimeMilliseconds() * 1000;
                writer.WriteValue(microseconds.ToString());
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Read the value as string (assuming microsecond precision timestamp)
            var timestampStr = (string)reader.Value;

            if (timestampStr.Length == 14)  // Adjust length to match your format
            {
                try
                {
                    // Parse the timestamp from the server (YYYYMMDDHHMMSS)
                    string dateString = timestampStr.Substring(0, 8);  // YYYYMMDD
                    string timeString = timestampStr.Substring(8, 6);     // HHMMSS

                    string combinedString = dateString + " " + timeString;

                    // Parse the combined string into a DateTime object
                    DateTime parsedDateTime = DateTime.ParseExact(combinedString, "yyyyMMdd HHmmss", CultureInfo.InvariantCulture);

                    return parsedDateTime;
                }
                catch (Exception ex)
                {
                    throw new JsonSerializationException("Invalid date format.", ex);
                }
            }

            return DateTime.MinValue;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DateTime);
        }
    }
}
