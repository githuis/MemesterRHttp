using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using RHttpServer;
using RHttpServer.Plugins;
using RHttpServer.Plugins.Default;
using RHttpServer.Plugins.External;
using RHttpServer.Response;

namespace MemesterRHttp
{
    class Program
    {
        private static readonly object ReportLock = new object();

        static void Main(string[] args)
        {
            var server = new HttpServer(3000, 3, "./public", false) { CachePublicFiles = true };
            var db = new SimpleSQLiteDatatase("db.sqlite");
            //db.DropTable<Meme>();
            db.CreateTable<Meme>();
            db.CreateTable<User>();
            db.CreateTable<Report>();
            var dict = LoadMemes(db.GetTable<Meme>());

            var crawler = new Crawler(dict, db, TimeSpan.FromMinutes(30));
            server.CachePublicFiles = true;

            var rand = new Random();

            server.Get("/", (req, res) =>
            {
                var m = rand.Next(0, dict.Length - 1);
                var meme = dict[m];
                res.Redirect("/meme/" + meme.OrgId);
            });

            server.Get("/*", (req, res) =>
            {
                res.Redirect("/404");
            });

            server.Get("/404", (req, res) =>
            {
                res.RenderPage("pages/404.ecs", null);
            });

            server.Get("/instameme", (req, res) =>
            {
                var m = rand.Next(0, dict.Length);
                var meme = dict[m];
                res.Redirect(meme.WebPath);
            });

            server.Get("/user/:user", (req, res) =>
            {
                var par = new RenderParams();
                var usr = db.Get<User>(req.Params["user"]);
                par.Add("memes", usr.Votes.Keys);
                res.RenderPage("pages/acc/account.ecs", par);
            });

            server.Get("/user/:user/liked", (req, res) =>
            {
                var user = req.Params["user"];
                var items = req.Queries["p"];
                if (string.IsNullOrEmpty(items)) items = "1";
                var u = db.Get<User>(user);
                int i;
                if (u == null || !int.TryParse(items, out i))
                {
                    res.SendString("no");
                    return;
                }

                var liked = u.Votes.Keys.Skip(i - 1 * 20).Take(20);
                res.SendJson(liked);
            });

            server.Get("/meme/:meme", (req, res) =>
            {
                var memeid = req.Params["meme"];
                if (string.IsNullOrWhiteSpace(memeid))
                {
                    res.Redirect("/404");
                    return;
                }
                Meme meme;
                if (!dict.TryGetValue(memeid, out meme))
                {
                    res.Redirect("/404");
                    return;
                }
                var rp = new RenderParams
                {
                    {"title", meme.Title},
                    {"score", meme.Score},
                    {"path", meme.WebPath},
                    {"thread", meme.Thread},
                    {"thumb", meme.WebThumb},
                    {"orgId", meme.OrgId },
                    {"memes", dict.Length }
                };
                res.RenderPage("pages/index.ecs", rp);
            });

            server.Post("/meme/:meme/vote", (req, res) =>
            {
                var u = req.Queries["user"];
                var p = req.Queries["pass"];
                var m = req.Params["meme"];
                var v = req.Queries["val"];
                int val;

                if (string.IsNullOrWhiteSpace(u) || string.IsNullOrWhiteSpace(m) || string.IsNullOrWhiteSpace(v) || string.IsNullOrWhiteSpace(p))
                {
                    res.SendString("no");
                    return;
                }

                if (!int.TryParse(v, out val) || val < -1 || val > 1)
                {
                    res.SendString("no");
                    return;
                }


                Meme meme;
                if (!dict.TryGetValue(m, out meme))
                {
                    res.SendString("no");
                    return;
                }

                var user = db.Get<User>(u);
                if (user == null || user.PassHash != p)
                {
                    res.SendString("no");
                    return;
                }

                int cv;
                meme.Vote(user.Votes.TryGetValue(m, out cv) ? CalcVote(cv, val) : val);
                user.Votes[m] = val;
                db.Update(meme);
                db.Update(user);
                res.SendString("ok");
            });

            server.Post("/meme/:meme/report", (req, res) =>
            {
                var m = req.Params["meme"];
                var body = req.GetBodyPostFormData();
                var rn = body["rn"];
                var reason = body["reason"];
                var email = body["email"];

                if (string.IsNullOrWhiteSpace(rn) || ((rn == "2" || rn == "4") && string.IsNullOrWhiteSpace(reason)))
                {
                    res.SendString("no");
                    return;
                }

                if (string.IsNullOrWhiteSpace(email)) email = "$ANON";

                int rval;
                if (!int.TryParse(rn, out rval) || rval < 0 || rval > 4)
                {
                    res.SendString("no");
                    return;
                }
                
                Meme meme;
                if (!dict.TryGetValue(m, out meme))
                {
                    res.SendString("no");
                    return;
                }
                var rr = (Report.ReportReason) rval;
                // will do for now
                lock (ReportLock)
                {
                    File.AppendAllText("reported.txt", $"{m}\t\t{email}\t\t{rr}\t\t{reason}\n");
                }
                db.Insert(new Report(m, rr, email, reason));
                res.SendString("ok");
            });

            server.Post("/meme/:meme/remove", async (req, res) =>
            {
                var pass = req.Queries["pwd"];
                var m = req.Params["meme"];
                var pwd = File.ReadAllText("rmpwd");

                Meme meme;
                if (pwd != pass || !dict.TryGetValue(m, out meme))
                {
                    var r = rand.Next(750, 3000);
                    await Task.Delay(r);
                    res.SendString("no");
                    return;
                }
                await Task.Delay(500);
                db.Delete<Meme>(meme.OrgId);
                dict.Remove(meme);
                File.Delete(meme.Path);
                File.Delete(meme.Thumb);
            });

            server.Get("/thread/:thread", (req, res) =>
            {
                var tid = req.Params["thread"];
                tid = HttpUtility.UrlDecode(tid);
                var pars = new RenderParams
                {
                    {"thread", tid }
                };
                res.RenderPage("pages/thr/thread.ecs", pars);
            });

            server.Post("/thread/:thread", (req, res) =>
            {
                var tid = req.Params["thread"];
                tid = HttpUtility.UrlDecode(tid);
                var memes = db.Find<Meme>(m => m.Thread == tid).Select(m => m.OrgId);
                res.SendJson(memes);
            });

            server.Post("/reportedmemes", async (req, res) =>
            {
                var body = req.GetBodyPostFormData();
                var pass = body["pwd"];
                var pwd = File.ReadAllText("rmpwd");
                if (pass != pwd)
                {
                    var r = rand.Next(750, 1500);
                    await Task.Delay(r);
                    res.SendString("no");
                    return;
                }
                var reports = db.GetTable<Report>().ToList();
                res.SendJson(reports);
            });

            server.Get("/multimeme", (req, res) =>
            {
                var size = req.Queries["size"];
                if (string.IsNullOrEmpty(size) || !Regex.IsMatch(size, "[0-9]+x[0-9]+", RegexOptions.Compiled)) size = "3x3";
                var split = size.Split('x');
                var h = int.Parse(split[0]);
                var w = int.Parse(split[1]);
                if (h > 4) h = 4;
                if (w > 5) w = 5;
                var tot = h * w;
                var limit = tot * 3;
                var l = dict.Length;
                if (limit > l) limit = l;
                var r = new Random();
                var list = new List<Meme>();
                for (int i = 0; i < limit; i++)
                {
                    list.Add(dict[r.Next(0, l)]);
                }

                res.RenderPage("./pages/multimeme.ecs", new RenderParams
                {
                    {"h", h},
                    {"w", w},
                    {"m", list}
                });
            });
            
            server.Post("/login", (req, res) =>
            {
                var login = req.GetBodyPostFormData();
                var uid = login["usr"];
                var pwd = login["pwd"];
                if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(pwd))
                {
                    res.SendString("no");
                    return;
                }

                var user = db.FindOne<User>(u => u.Username == uid);
                if (user == null || user.PassHash != pwd)
                {
                    res.SendString("no");
                    return;
                }
                res.SendString("ok");
            });

            server.Get("/register", (req, res) =>
            {
                res.RenderPage("pages/reg/register.ecs", null);
            });

            server.Post("/register", (req, res) =>
            {
                var data = req.GetBodyPostFormData();
                if (!data.ContainsKey("username") || !data.ContainsKey("password"))
                    {
                    res.SendString("no");
                    return;
                }
                var un = data["username"];
                var user = db.FindOne<User>(u => u.Username == un);
                if (user != null)
                {
                    res.SendString("no");
                    return;
                }
                user = new User
                {
                    Username = un,
                    PassHash = data["password"]
                };
                db.Insert(user);
                res.SendString("ok");
            });

            crawler.Start();

            server.InitializeDefaultPlugins(true, true, new SimpleHttpSecuritySettings(1, 100, 5));
            server.Start();
        }

        private static MemeDictionary LoadMemes(IEnumerable<Meme> memes)
        {
            var retVal = new MemeDictionary();
            foreach (var meme in memes)
            {
                retVal.Add(meme);
            }

            Console.WriteLine("Memes loaded!");
            return retVal;
        }
        
        private static int CalcVote(int cv, int nv)
        {
            switch (cv)
            {
                case -1:
                    switch (nv)
                    {
                        case 0:
                            return 1;
                        case 1:
                            return 2;
                    }
                    break;
                case 0:
                    switch (nv)
                    {
                        case -1:
                            return -1;
                        case 1:
                            return 1;
                    }
                    break;
                case 1:
                    switch (nv)
                    {
                        case 0:
                            return -1;
                        case -1:
                            return -2;
                    }
                    break;
            }
            return 0;
        }


        
    }

    class MemeDictionary
    {
        private readonly ConcurrentDictionary<string, Meme> _dict = new ConcurrentDictionary<string, Meme>();
        private readonly List<Meme> _list = new List<Meme>();

        public void Add(Meme meme)
        {
            _dict.TryAdd(meme.OrgId, meme);
            _list.Add(meme);
        }
        
        public void Remove(Meme meme)
        {
            Meme m;
            _dict.TryRemove(meme.OrgId, out m);
            _list.Remove(meme);
        }

        public int Length => _list.Count;

        public bool TryGetValue(string id, out Meme meme)
        {
            return _dict.TryGetValue(id, out meme);
        }

        public Meme this[int index] => _list[index];

        public bool Contains(string id) => _dict.ContainsKey(id);
    }
}
