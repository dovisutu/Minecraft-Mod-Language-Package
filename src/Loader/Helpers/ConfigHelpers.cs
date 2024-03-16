using Loader.Converters;
using Loader.Models;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace Loader.Helpers
{
    /// <summary>
    /// 配置相关的工具类
    /// </summary>
    public static class ConfigHelpers
    {
        internal static JsonSerializerOptions persistentRegexOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = {new RegexPersistentReplaceableConverter()}
        };

        internal static JsonSerializerOptions temporaryRegexOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new RegexTemporaryReplaceableConverter() }
        };

        internal static JsonSerializerOptions policyOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        /// <summary>
        /// 从给定的命名空间获取局域配置
        /// </summary>
        /// <param name="directory">命名空间目录</param>
        /// <returns>若文件存在，返回<see cref="FloatingConfig"/>；否则，返回<see langword="null"/></returns>
        public static FloatingConfig? RetrieveLocalConfig(DirectoryInfo directory)
        {
            var configFile = directory.GetFiles("local-config.json").FirstOrDefault();

            if (configFile is null) return null;

            using var stream = configFile.OpenRead();
            try
            {
                return JsonSerializer.Deserialize<FloatingConfig>(
                    stream, temporaryRegexOptions);
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException($"局域配置文件{configFile.FullName}读取失败。", exception);
            }
        }

        /// <summary>
        /// 从仓库根目录获取全局配置
        /// </summary>
        /// <param name="version">打包版本，用于定位全局配置</param>
        public static Config RetrieveConfig(string version)
        {
            var configPath = $"./config/packer/{version}.json";
            var stream = File.OpenRead($"./config/packer/{version}.json");

            try
            {
                return JsonSerializer.Deserialize<Config>(
                    stream, persistentRegexOptions)!;
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException($"配置文件 {configPath} 读取失败。", exception);
            }
        }

        /// <summary>
        /// 从给定的命名空间获取策略内容。
        /// </summary>
        /// <param name="directory">命名空间所在目录。</param>
        /// <returns>若文件存在，返回对应的内容；否则，返回<c>[ Direct ]</c>。</returns>
        /// <exception cref="InvalidDataException">策略文件为 null。</exception>
        /// <exception cref="InvalidOperationException">策略文件解析失败。</exception>
        public static List<PackerPolicy> RetrievePolicies(DirectoryInfo directory)
        {
            var policyFile = directory.GetFiles("packer-policy.json").FirstOrDefault();

            if (policyFile is null)
                return
                [
                    new PackerPolicy { Type = PackerPolicyType.Direct }
                ];

            using var stream = policyFile.OpenRead();

            using var reader = policyFile.OpenText();
            try
            {
                var result = JsonSerializer.Deserialize<List<PackerPolicy>>(
                    stream, policyOptions)
                    // 这个null检查我也不知道有没有用...
                    ?? throw new InvalidDataException($"策略文件 {policyFile.FullName} 为 null 值。");
                return result;
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException($"策略文件 {policyFile.FullName} 读取失败。", exception);
            }
        }
    }
}
