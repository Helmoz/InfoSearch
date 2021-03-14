using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace InfoSearch.Lib
{
    public static class TextCleaner
    {
        private const string EmptySymbol = " ";
        private static string LinksRegex { get; } = @"\bhttps?:\/\/(www\.)?[-a-zа-я0-9@:%._\+~#=]{2,256}\.[a-zа-я]{2,6}\b([-a-zа-я0-9@:%_\+.~#?&//=]*)\b";
        private static string ScriptRegex { get; set; } = "<script.*?script>";
        private static string CssRegex { get; set; } = "<style.*?style>";
        private static string UnicodeSymbolsRegex { get; set; } = "&#.*?;";
        private static string TagsRegex { get; set; } = @"<(.|\n)*?>";
        private static string ExtraCarriageRegex { get; set; } = "[\r\n]+";
        private static string SingleDigitsRegex { get; set; } = @"\b\d+\b";
        private static string WhitespaceRegex { get; set; } = @"\s+";

        public static string Clean(this string source)
        {
            var removes = new List<Remove>()
            {
                new(LinksRegex),
                new(ScriptRegex, RegexOptions.Singleline),
                new(CssRegex, RegexOptions.Singleline),
                new(UnicodeSymbolsRegex),
                new(TagsRegex),
                new(ExtraCarriageRegex, RegexOptions.None, "\r\n"),
                new(SingleDigitsRegex),
                new(WhitespaceRegex),
            };

            return removes
                .Aggregate(source, (current, remove) => 
                    Regex.Replace(current, remove.Regex, remove.ReplaceSymbol, remove.RegexOptions));
        }

        private record Remove
        {
            public string Regex { get; }

            public string ReplaceSymbol { get; }

            public RegexOptions RegexOptions { get; }

            public Remove(string regex, RegexOptions regexOptions = RegexOptions.None, string replaceSymbol = EmptySymbol) =>
                (Regex, ReplaceSymbol, RegexOptions) = (regex, replaceSymbol, regexOptions);
        }
    }
}