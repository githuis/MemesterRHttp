using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Frapper;
using RHttpServer.Plugins.External;

namespace MemesterRHttp
{
    class Crawler
    {
        private const string MemePath = "./public/memes";
        private readonly ConcurrentDictionary<string, Meme> _dict;
        private readonly SimpleSQLiteDatatase _db;
        private readonly TimeSpan _interval;
        private static readonly FFMPEG FFMPEG = new FFMPEG("C:\\ffmpeg-3.2-win64-shared\\bin\\ffmpeg.exe");

        public Crawler(ConcurrentDictionary<string, Meme> dict, SimpleSQLiteDatatase db, TimeSpan interval)
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
                    Parallel.ForEach(memes, CheckIfExists);
                    await Task.Delay(_interval);
                }
            });
        }
        
        private void CheckIfExists(CMeme cmeme)
        {
            if (_dict.ContainsKey(cmeme.Title) || cmeme.Url.EndsWith("gif")) return;
            var meme = DownloadMeme(cmeme);
            _dict.TryAdd(meme.OrgId, meme);
            _db.Insert(meme);
        }


        public static IEnumerable<CMeme> Crawl()
        {
            var list = new List<CMeme>();
            var wc = new WebClient();
            var html = wc.DownloadString("http://boards.4chan.org/wsg/");
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            var nodes = doc.DocumentNode.SelectNodes("//div[@class='fileText']/a");
            foreach (var node in nodes)
            {
                string id = node.InnerText.Replace(".webm", "").Replace(".gif", "");
                string re = node.Attributes["href"].Value;
                if (!re.StartsWith("http")) re = "http:" + re;
                list.Add(new CMeme
                {
                    Title = id,
                    Url = re
                });
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
                OrgId = Path.GetFileNameWithoutExtension(filename),
                Ext = Path.GetExtension(filename),
                Title = meme.Title
            };
            CreateThumb(m);
            return m;
        }

        private static void CreateThumb(Meme meme)
        {
            var res = FFMPEG.RunCommand($"-i {meme.Path} -ss 00:00:01.435 -f image2 -vframes 1 {meme.Thumb}");
        }
    }
}