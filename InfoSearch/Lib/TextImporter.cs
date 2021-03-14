using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HtmlAgilityPack;

namespace InfoSearch.Lib
{
    public class TextImporter
    {
        public IEnumerable<string> ImportTexts(string dir)
        {
            var files = Directory.GetFiles(dir).Where(f => f.EndsWith(".txt"));

            foreach (var f in files)
            {
                var fileContent = File.ReadAllText(f, Encoding.UTF8);
                var document = new HtmlDocument();
                document.LoadHtml(fileContent);
                var body = document.GetElementbyId("bodyContent").InnerHtml;

                yield return body;
            }
        }
    }
}