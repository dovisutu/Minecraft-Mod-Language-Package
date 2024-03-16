using Loader.Extensions;
using Loader.Helpers;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace Loader.Models.Providers
{
    /// <summary>
    /// 一般文件的提供器。不提供合并、替换支持
    /// </summary>
    /// <remarks>
    /// 对于非文本文件，使用该类
    /// </remarks>
    /// <remarks>
    /// 从给定的<see cref="FileInfo" />构造提供器
    /// </remarks>
    /// <param name="sourceFile">源文件的引用</param>
    /// <param name="destination">目标地址</param>
    public class RawFile(FileInfo sourceFile, string destination) : IResourceFileProvider
    {
        /// <summary>
        /// 文件的源地址
        /// </summary>
        public FileInfo SourceFile { get; } = sourceFile;

        /// <inheritdoc/>
        public string Destination { get; } = destination;


        /// <inheritdoc/>
        public IResourceFileProvider ReplaceDestination(IRegexReplaceable searchPattern, string replacement)
            => new RawFile(SourceFile,
                           searchPattern.Replace(Destination,
                                         replacement));

        /// <inheritdoc/>
        public async Task WriteToArchive(ZipArchive archive)
        {
            var destination = Destination.NormalizePath();

            archive.ValidateEntryDistinctness(destination);

            // 为什么这ZipArchive.CreateEntryFromFile没有Async变种...只有手动实现了
            using var source = SourceFile.OpenRead();
            using var entry = archive.CreateEntry(destination)
                                     .Open();
            await source.CopyToAsync(entry);
        }
    }
}
