using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using LiteDB;

namespace MemesterCore
{
    class Crawler
    {
        private const string MemePath = "./public/memes";
        private readonly MemeDictionary _dict;
        private readonly TimeSpan _interval;
        private readonly Thread _thread;
        private static readonly FFMpeg FFMPEG = new FFMpeg("ffmpeg");

        private readonly LiteCollection<Meme> _db;

        private readonly DeleteManager _deleteManager;

        public Crawler(LiteCollection<Meme> db, DeleteManager delMan, MemeDictionary md,  TimeSpan interval)
        {
            _db = db;
            _interval = interval;
            _thread = new Thread(InternalCrwalerLoop);
            _deleteManager = delMan;
            _dict = md;
        }

        public void Start()
        {
            _thread.Start();
        }

        private async void InternalCrwalerLoop()
        {
            while (true)
            {
                Console.WriteLine("Started downloading");
                var memes = await Crawl();
                memes = memes.Where(MemeFilter).ToList();
                _deleteManager.MakeRoom(memes.Count);
                Parallel.ForEach(memes, async m => await Download(m));
                Console.WriteLine("Done downloading for now");
                await Task.Delay(_interval);
            }
        }

        private bool MemeFilter(CMeme cm)
        {
            if (cm.Url.EndsWith(".gif"))
                return false;
            if (_dict.Contains(cm))
                return false;


            return true;
        }

        private async Task Download(CMeme cmeme)
        {
            try
            {
                var meme = await DownloadMeme(cmeme);
                _dict.Add(meme);
                _db.Insert(meme);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }


        private static async Task<string> DownloadHtml()
        {
            try
            {
                using (var client = new HttpClient
                {
                    DefaultRequestHeaders =
                    {
                        {"User-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.96 Safari/537.36" }
                    }
                })
                {
                    using (HttpResponseMessage response = await client.GetAsync("http://boards.4chan.org/wsg/", HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        var html = await response.Content.ReadAsStringAsync();
                        return html;
                    }
                }
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        public static async Task<List<CMeme>> Crawl()
        {
            var list = new List<CMeme>();
            var html = await DownloadHtml();
            var doc = new HtmlDocument();
            try
            {
                doc.LoadHtml(html);
                IEnumerable<HtmlNode> threads = doc.DocumentNode.QuerySelectorAll("div.thread");
                if (threads == null) return list;
                threads = threads.Skip(1);
                foreach (var node in threads)
                {
                    var split = node.QuerySelector("a.replylink").Attributes["href"].Value.Substring(7).Split('/');
                    var tid = split[0];
                    var name = split[1];
                    var files = node.QuerySelectorAll("div.file");
                    foreach (var htmlNode in files)
                    {
                        var tit = htmlNode.QuerySelector("a").InnerText;
                        if (tit.Contains("(...)")) tit = htmlNode.QuerySelector("a").Attributes["title"].Value;
                        var href = htmlNode.QuerySelector("a.fileThumb").Attributes["href"].Value;
                        if (href.EndsWith(".gif")) continue;
                        list.Add(new CMeme
                        {
                            Thread = WebUtility.HtmlDecode(name).Replace("(...)", ""),
                            ThreadId = long.Parse(tid),
                            Title = WebUtility.HtmlDecode(tit).Replace(".webm", ""),
                            Url = "http:" + href,
                            OrgId = long.Parse(href.Substring(href.LastIndexOf("/") + 1).Replace(".webm", "")),
                        });
                    }
                }
            }
            catch (Exception e)
            {
            }
            return list;
        }
        
        public static async Task<Meme> DownloadMeme(CMeme meme)
        {
            var filename = meme.Url.Substring(meme.Url.LastIndexOf("/") + 1);
            var filepath = Path.Combine(MemePath, filename);
            var request = new HttpRequestMessage(HttpMethod.Get, meme.Url);
            using (var wc = new HttpClient())
            using (var response = await wc.SendAsync(request))
            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var file = File.Create(filepath))
                await stream.CopyToAsync(file);
            var m = new Meme
            {
                OrgId = meme.OrgId,
                Title = meme.Title,
                ThreadName = meme.Thread,
                ThreadId = meme.ThreadId
            };
            CreateThumb(m);
            return m;
        }

        private static void CreateThumb(Meme meme)
        {
            FFMPEG.ExecuteAsync($"-hide_banner -loglevel panic -ss 00:00:00.9 -i {meme.Path} -vf scale=-1:160 -q:v 10 -f image2 -vframes 1 {meme.Thumb} -y");
        }
    }
}