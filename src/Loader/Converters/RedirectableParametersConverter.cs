using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

// 本文件目前弃置，因为暂时没看到什么合适的用处。以后可能会做。

//namespace Loader.Converters
//{
//    // 组合文件参数使用“可重定向”格式：
//    // - object
//    //   - redirect bool[可选] 确定是否需重定向。若为false或不存在，读取字面量；否则，读取重定向文件。
//    //   - 若未重定向：
//    //     - 键参数 -> 值参数
//    //   - 若重定向：
//    //     - source string 参数文件的完整地址


//    using ParameterListType = List<Dictionary<string, string>>;
//    using ParameterType = Dictionary<string, string>;

//    internal class RedirectableParametersConverter : JsonConverter<ParameterListType>
//    {
//        internal JsonSerializerOptions nestedOptions = new()
//        {
//            Converters = { new ParameterDiscriminateConverter() }
//        };

//        public override ParameterListType? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
//            => JsonSerializer.Deserialize<ParameterListType>(ref reader, nestedOptions);

//        public override void Write(Utf8JsonWriter writer, ParameterListType value, JsonSerializerOptions options)
//            => throw new NotSupportedException();

//        internal class ParameterDiscriminateConverter : JsonConverter<ParameterType>
//        {

//            private static readonly JsonConverter<ParameterType> s_defaultConverter =
//        (JsonConverter<ParameterType>)JsonSerializerOptions.Default.GetConverter(typeof(ParameterType));

//            public override ParameterType? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
//            {
//                switch (reader.TokenType)
//                {
//                    case JsonTokenType.StartObject: return ReadSingular(ref reader, typeToConvert, options);
//                    case JsonTokenType.StartArray: return ReadMultiple(ref reader, typeToConvert, options);
//                    default: throw new JsonException();
//                }
//            }

//            internal ParameterType? ReadSingular(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
//            {
//                var readerClone = reader; // 值类型，赋值 == 复制
//                if (IsRedirected(ref readerClone, out var source))
//                {
//                    var file = new FileInfo(source);
//                    using var stream = file.OpenRead();
//                    return JsonSerializer.Deserialize<ParameterType>(stream, options);
//                }
//                else
//                {
//                    return s_defaultConverter.Read(ref reader, typeToConvert, options);
//                }
//            }

//            internal ParameterType? ReadMultiple(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
//            {
//                var parameters = JsonSerializer.Deserialize<ParameterListType>(ref reader, options);
//                return parameters!.SelectMany(_ => _)
//                                  .DistinctBy(_ => _.Key)
//                                  .ToDictionary(_ => _.Key, _ => _.Value);

//            }

//            internal bool IsRedirected(ref Utf8JsonReader reader, [MaybeNullWhen(false)] out string source)
//            {
//                if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();
//                source = null;
//                bool result = false;
//                while (reader.Read())
//                {
//                    // 对象末尾。此时应仍未找到 redirect 属性
//                    if (reader.TokenType == JsonTokenType.EndObject) return false;
//                    // 属性键
//                    if (reader.TokenType == JsonTokenType.PropertyName)
//                    {
//                        string? propertyName = reader.GetString();
//                        // 无关属性。忽略
//                        if (propertyName != "redirect" || propertyName != "source") continue;

//                        reader.Read();
//                        if (propertyName == "redirect")
//                        {
//                            switch (reader.TokenType)
//                            {
//                                // 属性值为 Bool
//                                case JsonTokenType.True: result = true; break;
//                                case JsonTokenType.False: return false;
//                                // 属性值为 String。无关属性
//                                case JsonTokenType.String: continue;
//                                // 其他类型。格式存在问题
//                                default: throw new JsonException();
//                            }
//                        }

//                        if (propertyName == "source")
//                        {
//                            switch (reader.TokenType)
//                            {
//                                case JsonTokenType.String: source = reader.GetString()!; break;
//                                default: throw new JsonException();
//                            }
//                        }
//                    }
//                    if (result == true && source != null) return true;
//                    // 对象中，但非属性键。格式存在问题
//                    throw new JsonException();
//                }
//                // 文档末尾，但非对象末尾。格式存在问题
//                throw new JsonException();
//            }


//            public override void Write(Utf8JsonWriter writer, ParameterType value, JsonSerializerOptions options)
//                => throw new NotSupportedException();
//        }
//    }
//}
