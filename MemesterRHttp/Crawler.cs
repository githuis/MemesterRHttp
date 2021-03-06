﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Fizzler.Systems.HtmlAgilityPack;
using LiteDB;

namespace MemesterRHttp
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

        public Crawler(LiteCollection<Meme> db, DeleteManager delMan, TimeSpan interval)
        {
            _db = db;
            _interval = interval;
            _thread = new Thread(InternalCrwalerLoop);
            _deleteManager = delMan;
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
                var memes = Crawl().Where(MemeFilter).ToList();
                _deleteManager.MakeRoom(memes.Count);
                Parallel.ForEach(memes, CheckIfExists);
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

        private void CheckIfExists(CMeme cmeme)
        {
            try
            {
                var meme = DownloadMeme(cmeme);
                _dict.Add(meme);
                _db.Insert(meme);
            }
            catch (Exception)
            {

            }
        }


        public static IEnumerable<CMeme> Crawl()
        {
            var wc = new WebClient();
            var html = wc.DownloadString("http://boards.4chan.org/wsg/");
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            var threads = doc.DocumentNode.QuerySelectorAll("div.thread").Skip(1);
            foreach (var node in threads)
            {
                var tid = "";
                var name = "";
                if (true)
                {
                    var split = node.QuerySelector("a.replylink").Attributes["href"].Value.Substring(7).Split('/');
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
                    yield return new CMeme
                    {
                        Thread = HttpUtility.HtmlDecode(name).Replace("(...)", ""),
                        ThreadId = long.Parse(tid),
                        Title = HttpUtility.HtmlDecode(tit).Replace(".webm", ""),
                        Url = "http:" + href,
                        OrgId = long.Parse(href.Substring(href.LastIndexOf("/") + 1).Replace(".webm", "")),
                    };
                }
            }
        }


        public static Meme DownloadMeme(CMeme meme)
        {
            var filename = meme.Url.Substring(meme.Url.LastIndexOf("/") + 1);
            var filepath = Path.Combine(MemePath, filename);
            using (var wc = new WebClient())
            {
                wc.DownloadFile(meme.Url, filepath);
            }
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