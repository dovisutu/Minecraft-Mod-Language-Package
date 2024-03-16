using Loader.Models;

namespace Loader.Extensions
{
    public static class EnumerationExtension
    {
        public static IEnumerable<DirectoryInfo> EnumerateNamespaces(
            this string assetDirectory, IEnumerable<string> targetModIdentifiers, Config config)
            => from modDirectory in new DirectoryInfo(assetDirectory).EnumerateDirectories()
               let modIdentifier = modDirectory.Name
               where !targetModIdentifiers.Any()                                   // 未提供列表，全部打包
                   || targetModIdentifiers.Contains(modIdentifier)                 // 有列表，仅打包列表中的项
               where !config.Base.ExclusionMods.Contains(modIdentifier)            // 没有被明确排除
               from namespaceDirectory in modDirectory.EnumerateDirectories()
               let namespaceName = namespaceDirectory.Name
               where !config.Base.ExclusionNamespaces.Contains(namespaceName)      // 没有被明确排除
               where namespaceName.ValidateNamespace()                             // 不是非法名称
               select namespaceDirectory;

        public static IEnumerable<IResourceFileProvider> MergeProviders(
            this IEnumerable<IResourceFileProvider> providers)
            => from provider in providers
               group provider by provider.Destination into destinationGroup
               select destinationGroup
                   .Aggregate(seed: null as IResourceFileProvider,
                              (accumulate, next)
                                  => next.ApplyTo(
                                      accumulate));

        public static IEnumerable<IResourceFileProvider> ReplaceContent(
            this IEnumerable<IResourceFileProvider> providers, Config config)
            => from provider in providers
               select config.Floating.CharacterReplacement
                            .Aggregate(seed: provider,
                                       (accumulate, replacement)
                                           => accumulate.ReplaceContent(
                                               replacement.Key,
                                               replacement.Value));

        public static IEnumerable<IResourceFileProvider> ReplaceDestination(
            this IEnumerable<IResourceFileProvider> providers, Config config)
            => from provider in providers
               select config.Floating.DestinationReplacement
                            .Aggregate(seed: provider,
                                       (accumulate, replacement)
                                           => accumulate.ReplaceDestination(
                                               replacement.Key,
                                               replacement.Value));
    }
}
