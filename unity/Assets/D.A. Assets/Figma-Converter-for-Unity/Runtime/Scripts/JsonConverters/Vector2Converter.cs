#if JSONNET_EXISTS
using System;
using Newtonsoft.Json;
using UnityEngine;

namespace DA_Assets.FCU
{
    internal class Vector2Converter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var v = (Vector2)value;
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(v.x);
            writer.WritePropertyName("y");
            writer.WriteValue(v.y);
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                if (objectType != typeof(Vector2))
                {
                    throw new JsonSerializationException("Cannot convert type " + objectType + " to V2.");
                }

                return null;
            }

            var v = new Vector2();
            reader.Read();

            while (reader.TokenType == JsonToken.PropertyName)
            {
                string a = reader.Value.ToString();

                if (string.Equals(a, "x", StringComparison.OrdinalIgnoreCase))
                {
                    reader.Read();

                    if (reader.TokenType != JsonToken.Null)
                        v.x = (float)serializer.Deserialize(reader, typeof(float));
                }
                else if (string.Equals(a, "y", StringComparison.OrdinalIgnoreCase))
                {
                    reader.Read();

                    if (reader.TokenType != JsonToken.Null)
                        v.y = (float)serializer.Deserialize(reader, typeof(float));
                }
                else
                {
                    reader.Skip();
                }

                reader.Read();
            }

            return v;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Vector2);
        }
    }
}
#endif