using Loader.Helpers;
using Loader.Models;
using Loader.Models.Providers;
using System.Text.Json;

namespace Loader.Extensions
{
    using EvaluatorReturnType = IEnumerable<(IResourceFileProvider provider, ApplyOptions options)>;
    using ParameterType = Dictionary<string, JsonElement>;

    /// <summary>
    /// 用于处理\[namespace]层级的不同加载策略的拓展方法，以及一些辅助方法
    /// </summary>
    public static partial class DirectoryExtension
    {
        /// <summary>
        /// 加载策略所使用的标准方法代理
        /// </summary>
        /// <param name="namespaceDirectory">加载的基准位置</param>
        /// <param name="config">采用的全局配置</param>
        /// <param name="parameters">局部打包配置的附加参数</param>
        /// <returns>一个<see cref="Tuple"/>，第一参数为提供器的目标</returns>
        public delegate EvaluatorReturnType
            ProviderEvaluator(DirectoryInfo namespaceDirectory,
                              Config config,
                              ParameterType? parameters);

        /// <summary>
        /// 从给定的命名空间，基于当地的<c>packer-policy.json</c>
        /// 与<c>local-config.json</c>，遍历<see cref="IResourceFileProvider"/>
        /// </summary>
        /// <param name="namespaceDirectory">命名空间所在目录</param>
        /// <param name="config">所使用的<i>全局</i>配置</param>
        /// <returns></returns>
        public static IEnumerable<IResourceFileProvider> EnumerateProviders
            (this DirectoryInfo namespaceDirectory, Config config)
            => from enumeratedPair in namespaceDirectory.EnumerateRawProviders(config)
               group enumeratedPair by enumeratedPair.provider.Destination into providerGroup
               select providerGroup.Aggregate(
                   seed: null as IResourceFileProvider,
                   (accumulate, next)
                       => next.provider.ApplyTo(accumulate, next.options));


        #region Data
        /// <summary>
        /// 从<see cref="PackerPolicyType"/>到加载方法<see cref="ProviderEvaluator"/>的查询表
        /// </summary>
        internal static Dictionary<PackerPolicyType, ProviderEvaluator> evaluatorPolicyMap = new()
        {
            { PackerPolicyType.Direct, FromCurrentDirectory },      // 现场生成
            { PackerPolicyType.Indirect, FromSpecifiedDirectory },  // 给定目录
            { PackerPolicyType.Composition, FromComposition },      // 组合生成
            { PackerPolicyType.Singleton, FromSingleton },          // 单项文件
        };

        internal static readonly IRegexReplaceable assetPattern = new PersistentRegexStatement(@"(?<=^assets/)[^/]*(?=/)");
        #endregion

        #region Enumerations
        /// <summary>
        /// 遍历未经合并的文件，用于递归调用
        /// </summary>
        internal static EvaluatorReturnType EnumerateRawProviders(this DirectoryInfo namespaceDirectory, Config config)
        {
            try
            {
                return from policy in ConfigHelpers.RetrievePolicies(namespaceDirectory)
                       from enumeratedPair in evaluatorPolicyMap[policy.Type](
                           namespaceDirectory, config, policy.Parameters)
                       select enumeratedPair;
            }
            catch (InvalidOperationException exception)
            {
                throw new InvalidOperationException(
                    $"在从头枚举命名空间 {namespaceDirectory.FullName} 时，出现错误。", exception);
            }
        }

        internal static EvaluatorReturnType FromCurrentDirectory(DirectoryInfo namespaceDirectory,
                                                                 Config config,
                                                                 ParameterType? parameters)
        {
            var floatingConfig = ConfigHelpers.RetrieveLocalConfig(namespaceDirectory);
            var localConfig = config.Modify(floatingConfig);
            try
            {
                return from candidate in namespaceDirectory.EnumerateFiles("*", SearchOption.AllDirectories)
                       let relativePath = Path.GetRelativePath(namespaceDirectory.FullName,
                                                               candidate.FullName)
                                              .NormalizePath()
                       let fullPath = Path.GetRelativePath(".", candidate.FullName)
                       let destination = Path.Combine("assets", namespaceDirectory.Name, relativePath)
                                             .NormalizePath()
                       where !relativePath.IsPathForceExcluded(localConfig)            // [1] 排除路径   -- packer-policy等
                       where relativePath.IsPathForceIncluded(localConfig)             // [2] 包含路径   [单列]
                           || relativePath.IsDomainForceIncluded(localConfig)          // [3] 包含domain -- font/ textures/
                           || destination.IsInTargetLanguage(localConfig)              // [4] 语言标记   -- 含zh_cn的
                               && !relativePath.IsDomainForceExcluded(localConfig)     // [5] 排除domain [暂无]
                       let provider = CreateProviderFromFile(candidate, destination, localConfig)
                       select (provider, GetOptions(parameters));
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"在原位枚举命名空间 {namespaceDirectory.FullName} 时，出现错误。", exception);
            }
        }

        internal static EvaluatorReturnType FromSpecifiedDirectory(DirectoryInfo namespaceDirectory,
                                                                   Config config,
                                                                   ParameterType? parameters)
        {
            var redirect = parameters.GetParameterFromKey("source");

            try
            {
                var namespaceName = namespaceDirectory.Name;
                var redirectDirectory = new DirectoryInfo(redirect!);
                return from candidate in redirectDirectory.EnumerateRawProviders(config)
                       let provider = candidate.provider
                                               .ReplaceDestination(assetPattern, namespaceName)
                       select (provider, GetOptions(parameters));
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"在执行 [indirect] 策略自 {redirect} 枚举文件时，出现错误。", exception);
            }
        }

        internal static EvaluatorReturnType FromComposition(DirectoryInfo namespaceDirectory,
                                                            Config config,
                                                            ParameterType? parameters)
        {
            var compositionPath = parameters.GetParameterFromKey("source");
            var type = parameters.GetParameterFromKey("destType");

            IResourceFileProvider provider;

            try
            {
                var compositionFile = new FileInfo(compositionPath!);
#pragma warning disable CA2208 // 正确实例化参数异常
                provider = type switch
                {
                    "lang" => LangMappingHelper.CreateFromComposition(compositionFile),
                    "json" => JsonMappingHelper.CreateFromComposition(compositionFile),
                    _ => throw new ArgumentOutOfRangeException(
                        "destType", type, "组合策略的目标文件类型不正确。")
                };
#pragma warning restore CA2208 // 正确实例化参数异常
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"在执行 [composition] 策略自 {compositionPath} 加载文件时，出现错误。", exception);
            }
            yield return (provider, GetOptions(parameters));
        }

        internal static EvaluatorReturnType FromSingleton(DirectoryInfo namespaceDirectory,
                                                          Config config,
                                                          ParameterType? parameters)
        {
            var singletonPath = parameters.GetParameterFromKey("source");
            var relativePath = parameters.GetParameterFromKey("relativePath");

            var destination = Path.Combine("assets", namespaceDirectory.Name, relativePath)
                                  .NormalizePath();

            IResourceFileProvider provider;

            try
            {
                var file = new FileInfo(singletonPath);
                provider = CreateProviderFromFile(file, destination, config);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"在执行 [singleton] 策略自 {singletonPath} 加载文件时，出现错误。", exception);
            }
            yield return (provider, GetOptions(parameters));
        }

        #endregion

        internal static IResourceFileProvider CreateProviderFromFile(FileInfo file, string destination, Config _)
        {
            var extension = file.Extension;
            try
            {
                if (file.Directory!.Name == "lang")
                {
                    switch (extension)
                    {
                        case ".json": return JsonMappingHelper.CreateFromFile(file, destination);
                        case ".lang": return LangMappingHelper.CreateFromFile(file, destination);
                    };
                }
                return extension switch
                {
                    // 已知的文本文件类型
                    ".txt" or ".json" or ".md" => TextFile.Create(file, destination),
                    _ => new RawFile(file, destination)
                };
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"无法从文件 {file.FullName} 创建合理的提供器。", exception);
            }
        }

        internal static ApplyOptions GetOptions(ParameterType? parameters)
        {
            if (parameters is null) return default;
            return new(GetBoolOrDefalut("modifyOnly"), GetBoolOrDefalut("append"));

            bool GetBoolOrDefalut(string key)
            {
                if (!parameters.TryGetValue(key, out JsonElement value)) return false;
                return value.GetBoolean();
            }
        }
    }
}