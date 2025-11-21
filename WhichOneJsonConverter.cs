using System.Text.Json;
using System.Text.Json.Serialization;

namespace WhichOne;

public class WhichOneJsonConverter : JsonConverterFactory
{
	public override bool CanConvert(Type typeToConvert)
	{
		if (!typeToConvert.IsGenericType)
			return false;
		var genericType = typeToConvert.GetGenericTypeDefinition();
		return genericType == typeof(WhichOne<,>);
	}

	private class _whichOneJsonConverterInner<T1, T2> : JsonConverter<WhichOne<T1, T2>>
	{
		public override WhichOne<T1, T2> Read(ref Utf8JsonReader reader,
			Type typeToConvert,
			JsonSerializerOptions options)
		{
			using var doc = JsonDocument.ParseValue(ref reader);
			var root = doc.RootElement;
			if (!root.TryGetProperty("$type", out var typeProperty))
				throw new JsonException("Missing $type discriminator");
			var typeName = typeProperty.GetString();
			if (!root.TryGetProperty("value", out var valueProperty))
				throw new JsonException("Missing value property");
			if (typeName == typeof(T1).Name)
			{
				var value = JsonSerializer.Deserialize<T1>(
					valueProperty.GetRawText(), options);
				return WhichOne<T1, T2>.From(value);
			}
			if (typeName != typeof(T2).Name) throw new JsonException($"Unknown type discriminator: {typeName}");
			{
				var value = JsonSerializer.Deserialize<T2>(
					valueProperty.GetRawText(), options);
				return WhichOne<T1, T2>.From(value);
			}
		}

		public override void Write(Utf8JsonWriter writer,
			WhichOne<T1, T2> value,
			JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			value.Switch(
				t1 =>
				{
					writer.WriteString("$type", typeof(T1).Name);
					writer.WritePropertyName("value");
					JsonSerializer.Serialize(writer, t1, options);
				},
				t2 =>
				{
					writer.WriteString("$type", typeof(T2).Name);
					writer.WritePropertyName("value");
					JsonSerializer.Serialize(writer, t2, options);
				}
			);
			writer.WriteEndObject();
		}
	}

	public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		var typeArgs = typeToConvert.GetGenericArguments();
		var converterType = typeof(_whichOneJsonConverterInner<,>).MakeGenericType(typeArgs);
		return (JsonConverter)Activator.CreateInstance(converterType)!;
	}
}