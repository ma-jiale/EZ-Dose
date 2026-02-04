#if JSONNET_EXISTS
using System;
using Newtonsoft.Json;

namespace DA_Assets.FCU
{
    internal class ColorConverter: JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(UnityEngine.Color);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                if (objectType != typeof(UnityEngine.Color))
                {
                    throw new JsonSerializationException("Cannot convert type " + objectType + " to UnityEngine.Color");
                }

                return null;
            }

            var c = new UnityEngine.Color();
            reader.Read();

            while (reader.TokenType == JsonToken.PropertyName)
            {
                string a = reader.Value.ToString();

                if (string.Equals(a, "r", StringComparison.OrdinalIgnoreCase))
                {
                    reader.Read();

                    if (reader.TokenType != JsonToken.Null)
                        c.r = (float)serializer.Deserialize(reader, typeof(float));
                }
                else if (string.Equals(a, "g", StringComparison.OrdinalIgnoreCase))
                {
                    reader.Read();

                    if (reader.TokenType != JsonToken.Null)
                        c.g = (float)serializer.Deserialize(reader, typeof(float));
                }
                else if (string.Equals(a, "b", StringComparison.OrdinalIgnoreCase))
                {
                    reader.Read();

                    if (reader.TokenType != JsonToken.Null)
                        c.b = (float)serializer.Deserialize(reader, typeof(float));
                }
                else if (string.Equals(a, "a", StringComparison.OrdinalIgnoreCase))
                {
                    reader.Read();

                    if (reader.TokenType != JsonToken.Null)
                        c.a = (float)serializer.Deserialize(reader, typeof(float));
                }
                else
                {
                    reader.Skip();
                }

                reader.Read();
            }

            return c;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            UnityEngine.Color c = (UnityEngine.Color) value;
            writer.WriteStartObject();
            writer.WritePropertyName("r");
            writer.WriteValue(c.r);
            writer.WritePropertyName("g");
            writer.WriteValue(c.g);
            writer.WritePropertyName("b");
            writer.WriteValue(c.b);
            writer.WritePropertyName("a");
            writer.WriteValue(c.a);
            writer.WriteEndObject();
        }
    }
}
#endif