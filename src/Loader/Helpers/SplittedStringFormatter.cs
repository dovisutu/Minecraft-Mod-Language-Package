using System.Globalization;

namespace Loader.Helpers
{
    /// <summary>
    /// 实现自定义字符串格式“Z”。将输入项按<c>~~</c>分割，随后按照提供的参数取其中特定一段。<br>
    /// 如对输入"123~~456~~789"，Z0 -> 123, Z1 -> 456, Z2 -> 789。
    /// </summary>
    internal class SplittedStringFormatter : ICustomFormatter, IFormatProvider
    {
        public string Format(string? format, object? arg, IFormatProvider? formatProvider)
        {
            if(arg is string input
                && format is not null
                && format.StartsWith('Z')
                && format.Length > 1
                && int.TryParse(format[1..], out var index))
            {
                return input.Split("~~")[index];
            }

            // fallback
            try
            {
                return HandleOtherFormats(format, arg);
            }
            catch (FormatException e)
            {
                throw new FormatException(string.Format("The format of '{0}' is invalid.", format), e);
            }
            
        }

        public object? GetFormat(Type? formatType)
        {
            if (formatType == typeof(ICustomFormatter))
                return this;
            else
                return null;
        }

        private string HandleOtherFormats(string? format, object? arg)
        {
            if (arg is IFormattable formattable)
                return formattable.ToString(format, CultureInfo.CurrentCulture);
            else if (arg != null)
                return arg.ToString() ?? string.Empty;
            else
                return string.Empty;
        }
    }
}
