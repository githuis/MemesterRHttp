using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using RHttpServer;
using RHttpServer.Plugins.External;
using RHttpServer.Response;

namespace MemesterRHttp
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new HttpServer(5000, 3, "./public") { CachePublicFiles = true };
            var mongo = new SimpleMongoDBConnection("mongodb://localhost");
            server.RegisterPlugin<SimpleMongoDBConnection, SimpleMongoDBConnection>(mongo);
            var db = mongo.GetDatabase("memeste").GetCollection<BsonDocument>("");

            server.Post("/vote/:memeId", (req, res) =>
            {
                var memeId = req.Params["memeId"];
                var vote = req.ParseBody<MemeVote>();
                if (string.IsNullOrEmpty(memeId) || vote == null || InvalidVote(vote))
                {
                    res.SendString("no");
                    return;
                }

            });

            server.Get("/multimeme", async (req, res) =>
            {
                var size = req.Queries["size"];
                if (string.IsNullOrEmpty(size) || !Regex.IsMatch(size, "[0-9]+x[0-9]+", RegexOptions.Compiled)) size = "3x3";
                var split = size.Split('x');
                var h = int.Parse(split[0]);
                var w = int.Parse(split[1]);
                if (h > 4) h = 4;
                if (w > 5) w = 5;
                var tot = h*w;
                var limit = tot*3;
                FindOptions<BsonDocument> options = new FindOptions<BsonDocument> { Limit = limit };
                await db.Find(new BsonDocument()).ForEachAsync(d => Console.WriteLine(d));
                var list = new List<BsonDocument>();
                var r = new Random();
                int c = 0;
                while (list.Count < tot && ++c < limit)
                {
                    if (r.Next(0, 10) > 4) list.Add(memes.Current.FirstOrDefault());
                    memes.MoveNext();
                }
                res.RenderPage("./pages/multimeme.ecs", new RenderParams
                {
                    {"h", h},
                    {"w", w},
                    {"m", list}
                });
            });
            Crawler.StartCrawler();
        }

        private static bool InvalidVote(MemeVote vote)
        {
            if (string.IsNullOrWhiteSpace(vote.VCommand) || vote.VCommand != "u" || vote.VCommand != "d") return false;
            if (string.IsNullOrWhiteSpace(vote.Voter)) return false;
            if (vote.Voter[3] < 51 || vote.Voter[3] > 54) return false;
            if (vote.Voter[7] < 105 || vote.Voter[7] > 108) return false;
            if (vote.Voter[9] < 75 || vote.Voter[9] > 77) return false;
            return true;
        }
    }

    class MemeVote
    {
        public string Voter { get; set; }
        public string VCommand { get; set; }
    }

    class Crawler
    {
        public static void StartCrawler()
        {
            var memes = Crawl();
            Parallel.ForEach(memes, Downloader.DownloadMeme);
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
    }

    class Downloader
    {
        private const string MemePath = "./public/memes";
        public static void DownloadMeme(CMeme meme)
        {
            var filename = meme.Url.Substring(meme.Url.LastIndexOf("/") + 1);
            var filepath = Path.Combine(MemePath, filename);
            if (File.Exists(filepath)) return;
            var wc = new WebClient();
            wc.DownloadFile(new Uri(meme.Url), filepath);
        }
    }

    class CMeme
    {
        public string Title { get; set; }
        public string Url { get; set; }
    }
}
