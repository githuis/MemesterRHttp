using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using Fizzler.Systems.HtmlAgilityPack;
using RHttpServer.Plugins.External;

namespace MemesterRHttp
{
    class Crawler
    {
        private const string MemePath = "./public/memes";
        private readonly MemeDictionary _dict;
        private readonly SimpleSQLiteDatatase _db;
        private readonly TimeSpan _interval;
        private static readonly FFMpeg FFMPEG = new FFMpeg("ffmpeg");
        //private static readonly FFMpeg FFMPEG = new FFMpeg("C:\\ffmpeg-3.2-win64-shared\\bin\\ffmpeg.exe");

        public Crawler(MemeDictionary dict, SimpleSQLiteDatatase db, TimeSpan interval)
        {
            _dict = dict;
            _db = db;
            _interval = interval;
        }

        public void Start()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    var memes = Crawl();
                    Console.WriteLine("Started downloading");
                    Parallel.ForEach(memes, CheckIfExists);
                    Console.WriteLine("Done downloading for now");
                    await Task.Delay(_interval);
                }
            });
        }
        
        private void CheckIfExists(CMeme cmeme)
        {
            if (_dict.Contains(cmeme.OrgId) || cmeme.Url.EndsWith("gif")) return;
            var meme = DownloadMeme(cmeme);
            _dict.Add(meme);
            _db.Insert(meme);
        }


        public static IEnumerable<CMeme> Crawl()
        {
            var list = new List<CMeme>();
            var wc = new WebClient();
            var html = wc.DownloadString("http://boards.4chan.org/wsg/");
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            var threads = doc.DocumentNode.QuerySelectorAll("div.thread").Skip(1);
            foreach (var node in threads)
            {
                var tid = "";
                var name = node.QuerySelector("span.subject").InnerText;
                if (string.IsNullOrEmpty(name))
                {
                    var tt = node.QuerySelector("a.replylink");
                    name = tt.Attributes["href"].Value.Substring(7);
                    var split = name.Split('/');
                    tid = split[0];
                    name = split[1];
                }
                var files = node.QuerySelectorAll("div.file");
                foreach (var htmlNode in files)
                {
                    var tit = htmlNode.QuerySelector("a").InnerText;
                    if (tit.Contains("(...)")) tit = htmlNode.QuerySelector("a").Attributes["title"].Value;
                    var href = htmlNode.QuerySelector("a.fileThumb").Attributes["href"].Value;
                    if (href.EndsWith(".gif")) continue;
                    list.Add(new CMeme
                    {
                        Thread = HttpUtility.HtmlDecode(name).Replace("(...)", ""),
                        ThreadId = long.Parse(tid),
                        Title = HttpUtility.HtmlDecode(tit).Replace(".webm", ""),
                        Url = "http:" + href,
                        OrgId = long.Parse(href.Substring(href.LastIndexOf("/") + 1).Replace(".webm", "")),
                    });
                }
            }

            return list;
        }


        public static Meme DownloadMeme(CMeme meme)
        {
            var filename = meme.Url.Substring(meme.Url.LastIndexOf("/") + 1);
            var filepath = Path.Combine(MemePath, filename);
            var wc = new WebClient();
            wc.DownloadFile(new Uri(meme.Url), filepath);
            var m = new Meme
            {
                OrgId = meme.OrgId,
                Title = meme.Title,
                ThreadName = meme.Thread,
                ThreadId = long.Parse(meme.Thread)
            };
            CreateThumb(m);
            return m;
        }

        private static void CreateThumb(Meme meme)
        {
            var res = FFMPEG.Execute($"-i {meme.Path} -y -vf scale=-1:180 -ss 00:00:01.435 -f image2 -vframes 1 {meme.Thumb}");
            Console.WriteLine(res);
        }
    }
}