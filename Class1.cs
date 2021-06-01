using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace DictatedWords
{
    class DictatedWords
    {
        static readonly System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();

        string title;
        string raw_text;
        string words_html;
        string text_html = null;
        string answer_html = null;


        public static string get_pinyin(String word)
        {
            System.Net.Http.HttpResponseMessage response = client.GetAsync($"https://dict.baidu.com/s?wd={word}&ptype=zici").Result;
            response.EnsureSuccessStatusCode();
            string responseBody = response.Content.ReadAsStringAsync().Result;

            if (responseBody.Contains("抱歉：百度汉语中没有收录相关结果。"))
            {
                throw new MemberAccessException("词语不存在");
            }

            string match = System.Text.RegularExpressions.Regex.Match(responseBody, @"\[ .+ \]").Value;

            match = match.Replace("[ ", "");
            match = match.Replace(" ]", "");

            return match;
        }

        public static string format_words_text(string text)
        {
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\.", @" ");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", @" ");
            return text;
        }

        public static string pad_string(string text, int lengh)
        {
            int pad_lengh = lengh - text.Length;
            int left_pad_lengh = pad_lengh / 2;
            int right_pad_lengh = pad_lengh - left_pad_lengh;

            string left_pad_string = new(' ', left_pad_lengh);
            string right_pad_string = new string(' ', right_pad_lengh);

            return $@"{left_pad_string}{text}{right_pad_string}";
        }

        public DictatedWords(string words_text, string title)
        {
            this.title = title;
            this.raw_text = words_text;

            List<string> words_html = new();
            string[] words = format_words_text(words_text).Split(" ");
            foreach (string each_word in words)
            {
                string each_word_with_space = pad_string(each_word, each_word.Length * 3);
                string each_pinyin = get_pinyin(each_word);
                string html =
                    @$"<div class=""question""><p class=""pinyin"">{each_pinyin}</p><p class=""kuohao"">（</p><pre class=""kuohao answer"">{each_word_with_space}</pre><p class=""kuohao"">）</p></div>";
                words_html.Add(html);
            }

            this.words_html = string.Join("", words_html);
        }

        public string get_text_html()
        {
            if (this.text_html == null)
            {
                string words_html_string = string.Join("", this.words_html);
                this.text_html =
                    @$"<!doctype html><html lang=""zh-cn""><head><meta charset=""UTF-8""><meta content=""width=device-width, user-scalable=no, initial-scale=1.0, maximum-scale=1.0, minimum-scale=1.0""name=""viewport""><meta content=""ie=edge""http-equiv=""X-UA-Compatible""><style>h1{{margin:1em}}.question{{display:inline-block;font-size:20px;margin:0.5em}}.pinyin{{text-align:center;margin:0 0 0.5em}}.kuohao{{margin:0;display:inline-block}}.answer{{color:transparent}}</style><title>{this.title}</title></head><body><h1>{this.title}</h1>{this.words_html}</body></html>";
            }

            return this.text_html;
        }

        public string get_answer_html()
        {
            if (this.answer_html == null)
            {
                this.answer_html =
                    $@"<!doctype html><html lang=""zh-cn""><head><meta charset=""UTF-8""><meta content=""width=device-width, user-scalable=no, initial-scale=1.0, maximum-scale=1.0, minimum-scale=1.0""name=""viewport""><meta content=""ie=edge""http-equiv=""X-UA-Compatible""><style>h1{{margin:1em}}.question{{display:inline-block;font-size:20px;margin:0.5em}}.pinyin{{text-align:center;margin:0 0 0.5em}}.kuohao{{margin:0;display:inline-block}}</style><title>{this.title} 答案</title></head><body><h1>{this.title}</h1>{this.words_html}</body></html>";
            }

            return this.answer_html;
        }

        public MemoryStream get_zip_file()
        {
            using MemoryStream zip_stream = new();
            using (ZipArchive archive = new(zip_stream, ZipArchiveMode.Update))
            {
                //写入试卷
                ZipArchiveEntry TextHtmlEntry = archive.CreateEntry($"{this.title} 试卷.html");
                using StreamWriter TextHtmlWriter = new(TextHtmlEntry.Open());
                TextHtmlWriter.Write(get_text_html());

                //写入答案
                ZipArchiveEntry AnswerHtmlEntry = archive.CreateEntry($"{this.title} 答案.html");
                using StreamWriter AnswerHtmlWriter = new(AnswerHtmlEntry.Open());
                AnswerHtmlWriter.Write(get_answer_html());

                //写入原词
                ZipArchiveEntry RawTextEntry = archive.CreateEntry($"{this.title} 原词.txt");
                using StreamWriter RawTextWriter = new(RawTextEntry.Open());
                RawTextWriter.Write(raw_text);
            }

            return zip_stream;
        }

        public void WriteToFile(string file_name)
        {
            using FileStream real_file = new($"{this.title}.zip", FileMode.OpenOrCreate);
            byte[] file = get_zip_file().ToArray();
            real_file.Write(file);
        }
    }
}
