using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Deserializes every string through a pool, so a loaded world shares one instance per distinct value the way
/// a freshly simulated one does. Without it each occurrence of a record, artist or label id in the JSON becomes
/// its own copy; the 1969 fixture held ~5M strings, most of them repeats. Contents are unchanged (strings are
/// immutable), only reference identity differs. Use one instance per load and drop it afterwards.
/// </summary>
public sealed class PooledStringJsonConverter : JsonConverter<string> {
	private readonly Dictionary<string, string> pool = new(StringComparer.Ordinal);

	public override bool HandleNull => false;

	private string Pool(string value) {
		if (value == null) return null;
		if (pool.TryGetValue(value, out string shared)) return shared;
		pool.Add(value, value);
		return value;
	}

	public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
		Pool(reader.GetString());

	public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
		writer.WriteStringValue(value);

	public override string ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
		Pool(reader.GetString());

	public override void WriteAsPropertyName(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
		writer.WritePropertyName(value);

	/// <summary>A copy of <paramref name="options"/> that reads strings through a fresh pool and shares the empty array.</summary>
	public static JsonSerializerOptions For(JsonSerializerOptions options) {
		var pooled = new JsonSerializerOptions(options);
		pooled.Converters.Add(new PooledStringJsonConverter());
		pooled.Converters.Add(new SharedEmptyArrayConverterFactory());
		return pooled;
	}
}

/// <summary>Reads T[] as the default converter would, but an empty JSON array becomes Array.Empty&lt;T&gt;(): a loaded
/// world otherwise holds a separate zero-length array for every empty tag/mode list (millions late in a decade).</summary>
public sealed class SharedEmptyArrayConverterFactory : JsonConverterFactory {
	// Only arrays of strings, enums and primitives (the tag, mode and axis arrays): their element converters read a
	// single token, so this reads exactly what the default array converter would.
	public override bool CanConvert(Type typeToConvert) {
		if (!typeToConvert.IsArray || typeToConvert.GetArrayRank() != 1) return false;
		Type element = typeToConvert.GetElementType();
		return element == typeof(string) || element.IsEnum || element.IsPrimitive;
	}

	public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
		(JsonConverter)Activator.CreateInstance(typeof(Converter<>).MakeGenericType(typeToConvert.GetElementType()));

	private sealed class Converter<T> : JsonConverter<T[]> {
		public override T[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
			if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException($"Expected an array for {typeToConvert}.");
			var element = (JsonConverter<T>)options.GetConverter(typeof(T));
			List<T> items = null;
			while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
				(items ??= new List<T>()).Add(reader.TokenType == JsonTokenType.Null && default(T) == null
					? default : element.Read(ref reader, typeof(T), options));
			return items == null ? Array.Empty<T>() : items.ToArray();
		}

		public override void Write(Utf8JsonWriter writer, T[] value, JsonSerializerOptions options) {
			var element = (JsonConverter<T>)options.GetConverter(typeof(T));
			writer.WriteStartArray();
			foreach (T item in value) {
				if (item == null) writer.WriteNullValue();
				else element.Write(writer, item, options);
			}
			writer.WriteEndArray();
		}
	}
}
