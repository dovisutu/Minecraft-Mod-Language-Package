using Loader.Helpers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Loader.Converters
{
    internal class RegexPersistentReplaceableConverter : JsonConverter<IRegexReplaceable>
    {
        public override IRegexReplaceable? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => new PersistentRegexStatement(reader.GetString()!);

        public override IRegexReplaceable ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => new PersistentRegexStatement(reader.GetString()!);

        public override void Write(Utf8JsonWriter writer, IRegexReplaceable value, JsonSerializerOptions options)
            => throw new NotSupportedException();

        public override void WriteAsPropertyName(Utf8JsonWriter writer, [DisallowNull] IRegexReplaceable value, JsonSerializerOptions options)
            => throw new NotSupportedException();
    }

    internal class RegexTemporaryReplaceableConverter : JsonConverter<IRegexReplaceable>
    {
        public override IRegexReplaceable? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => new TemporaryRegexStatement(reader.GetString()!);

        public override IRegexReplaceable ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => new TemporaryRegexStatement(reader.GetString()!);

        public override void Write(Utf8JsonWriter writer, IRegexReplaceable value, JsonSerializerOptions options)
            => throw new NotSupportedException();

        public override void WriteAsPropertyName(Utf8JsonWriter writer, [DisallowNull] IRegexReplaceable value, JsonSerializerOptions options)
            => throw new NotSupportedException();
    }
}
