using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace InfoSearch.Lib
{
    public static class TextCleaner
    {
        private const string EmptySymbol = " ";

        public static string Clean(this string source)
        {
            return source
                .RemoveLinks()
                .RemoveScript()
                .RemoveCss()
                .RemoveUnicodeSymbols()
                .RemoveTags()
                .RemoveExtraCarriage()
                .RemoveSingleDigits()
                .RemoveWhitespace();
        }

        private static string RemoveScript(this string source)
        {
            return Regex.Replace(source, "<script.*?script>", EmptySymbol, RegexOptions.Singleline);
        }

        private static string RemoveCss(this string source)
        {
            return Regex.Replace(source, "<style.*?style>", EmptySymbol, RegexOptions.Singleline);
        }

        private static string RemoveUnicodeSymbols(this string source)
        {
            return Regex.Replace(source, "&#.*?;", EmptySymbol);
        }

        private static string RemoveTags(this string source)
        {
            return Regex.Replace(source, @"<(.|\n)*?>", EmptySymbol);
        }

        private static string RemoveExtraCarriage(this string source)
        {
            return Regex.Replace(source, "[\r\n]+", "\r\n");
        }

        private static string RemoveWhitespace(this string source)
        {
            return Regex.Replace(source, @"\s+", EmptySymbol);
        }
        
        private static string UrlRegex { get; } = @"\bhttps?:\/\/(www\.)?[-a-zа-я0-9@:%._\+~#=]{2,256}\.[a-zа-я]{2,6}\b([-a-zа-я0-9@:%_\+.~#?&//=]*)\b";

        private static string RemoveLinks(this string source)
        {
            return Regex.Replace(source, UrlRegex, EmptySymbol);
        }
        
        private static string RemoveSingleDigits(this string source)
        {
            return Regex.Replace(source, @"\b\d+\b", EmptySymbol);
        }
    }
}