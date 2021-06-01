using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace DictatedWords
{
    class DictatedWords
    {
        private static readonly System.Net.Http.HttpClient Client = new();

        readonly string title;
        readonly string rawText;
        private readonly string _wordsHtml = "";
        private string _textHtml;
        private string _answerHtml;
        
        static async Task<string> GetPinyinAsync(string word)
        {
            var response = await Client.GetAsync($"https://dict.baidu.com/s?wd={word}&ptype=zici");
            response.EnsureSuccessStatusCode();
            var responseBody = response.Content.ReadAsStringAsync().Result;

            if (responseBody.Contains("抱歉：百度汉语中没有收录相关结果。"))
            {
                throw new MemberAccessException("词语不存在");
            }

            var matchPinyin = System.Text.RegularExpressions.Regex.Match(responseBody, @"\[ .+ \]").Value;
            matchPinyin = matchPinyin.Replace("[ ", "");
            matchPinyin = matchPinyin.Replace(" ]", "");

            return matchPinyin;
        }

        public static string format_words_text(string text)
        {
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\.", @" ");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", @" ");
            return text;
        }

        public static string pad_string(string text, int lengh)
        {
            var padLengh = lengh - text.Length;
            var leftPadLengh = padLengh / 2;
            var rightPadLengh = padLengh - leftPadLengh;

            string leftPadString = new(' ', leftPadLengh);
            var rightPadString = new string(' ', rightPadLengh);

            return $@"{leftPadString}{text}{rightPadString}";
        }

        private DictatedWords(string wordsText, string title)
        {
            string[] words = format_words_text(wordsText).Split(" ");

            this.title = title;
            rawText = wordsText;
            
            //获取拼音并生成html
            var getPinyinTasks = words.Select(GetPinyinAsync).ToList();
            Task.WhenAll(getPinyinTasks);
            for (var index = 0; index < words.Length; index++)
            {
                var eachWord = words[index];
                var eachWordWithSpace = pad_string(eachWord, eachWord.Length * 3);
                var eachPinyin = getPinyinTasks[index].Result;
                
                _wordsHtml += @$"<div class=""question""><p class=""pinyin"">{eachPinyin}</p><p class=""kuohao"">（</p><pre class=""kuohao answer"">{eachWordWithSpace}</pre><p class=""kuohao"">）</p></div>";
            }
        }

        public string get_text_html()
        {
            return _textHtml ??= @$"<!doctype html><html lang=""zh-cn""><head><meta charset=""UTF-8""><meta content=""width=device-width, user-scalable=no, initial-scale=1.0, maximum-scale=1.0, minimum-scale=1.0""name=""viewport""><meta content=""ie=edge""http-equiv=""X-UA-Compatible""><style>h1{{margin:1em}}.question{{display:inline-block;font-size:20px;margin:0.5em}}.pinyin{{text-align:center;margin:0 0 0.5em}}.kuohao{{margin:0;display:inline-block}}.answer{{color:transparent}}</style><title>{title}</title></head><body><h1>{title}</h1>{_wordsHtml}</body></html>";
        }

        public string get_answer_html()
        {
            _answerHtml ??=
                $@"<!doctype html><html lang=""zh-cn""><head><meta charset=""UTF-8""><meta content=""width=device-width, user-scalable=no, initial-scale=1.0, maximum-scale=1.0, minimum-scale=1.0""name=""viewport""><meta content=""ie=edge""http-equiv=""X-UA-Compatible""><style>h1{{margin:1em}}.question{{display:inline-block;font-size:20px;margin:0.5em}}.pinyin{{text-align:center;margin:0 0 0.5em}}.kuohao{{margin:0;display:inline-block}}</style><title>{title} 答案</title></head><body><h1>{title}</h1>{_wordsHtml}</body></html>";

            return _answerHtml;
        }

        public MemoryStream get_zip_file()
        {
            using MemoryStream zipStream = new();
            using ZipArchive archive = new(zipStream, ZipArchiveMode.Update);
            //写入试卷
            var textHtmlEntry = archive.CreateEntry($"{this.title} 试卷.html");
            using StreamWriter textHtmlWriter = new(textHtmlEntry.Open());
            textHtmlWriter.Write(get_text_html());

            //写入答案
            var answerHtmlEntry = archive.CreateEntry($"{this.title} 答案.html");
            using StreamWriter answerHtmlWriter = new(answerHtmlEntry.Open());
            answerHtmlWriter.Write(get_answer_html());

            //写入原词
            var rawTextEntry = archive.CreateEntry($"{this.title} 原词.txt");
            using StreamWriter rawTextWriter = new(rawTextEntry.Open());
            rawTextWriter.Write(rawText);

            return zipStream;
        }

        public void WriteZipToFile(string fileName)
        {
            using FileStream realFile = new($"{title}.zip", FileMode.OpenOrCreate);
            var file = get_zip_file().ToArray();
            realFile.Write(file);
        }
    }
}