using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Loader.Helpers
{
    public interface IRegexReplaceable
    {
        public string Replace(string input, string replacement);
    }

    internal class PersistentRegexStatement([StringSyntax("regex")] string pattern) : IRegexReplaceable
    {
        internal Regex regex = new(pattern, RegexOptions.Singleline | RegexOptions.Compiled);

        public string Replace(string input, string replacement)
        {
            return regex.Replace(input, replacement);
        }
    }

    internal class TemporaryRegexStatement([StringSyntax("regex")] string pattern) : IRegexReplaceable
    {
        internal string regexPattern = pattern;

        public string Replace(string input, string replacement)
        {
            return Regex.Replace(input, regexPattern, replacement, RegexOptions.Singleline);
        }
    }
}
