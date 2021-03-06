﻿using LiteDB;
using RedHttpServerNet45;
using RedHttpServerNet45.Response;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.UI.WebControls;

namespace MemesterRHttp
{
    class Program
    {
        private static readonly object ReportLock = new object();

        static void Main(string[] args)
        {
            var server = new RedHttpServer(3000, "./public");
            var mdb = new LiteDatabase("memes.ldb").GetCollection<Meme>("Memes");
            var udb = new LiteDatabase("users.ldb").GetCollection<User>("Users");



            //var db = new SimpleSQLiteDatatase("db.sqlite");
            //db.CreateTable<Meme>();
            //db.CreateTable<User>();
            //db.CreateTable<Report>();
            var dict = LoadMemes(mdb.FindAll());
            var delMan = new DeleteManager(dict, mdb, 12);
            var crawler = new Crawler(mdb, delMan, TimeSpan.FromMinutes(5));
            var rand = new FastRandom();

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
                res.SendFile(meme.WebPath);
            });

            server.Get("/user/:user", (req, res) =>
            {
                var par = new RenderParams
                {
                    {"total", dict.Length}
                };
                res.RenderPage("pages/acc/account.ecs", par);
            });

            server.Get("/user/:user/liked", (req, res) =>
            {
                var user = req.Params["user"];
                var items = req.Queries["p"];
                if (string.IsNullOrEmpty(items)) items = "1";
                var u = udb.FindById(user);
                int i;
                if (u == null || !int.TryParse(items, out i))
                {
                    res.SendString("no");
                    return;
                }

                var liked = u.Votes.Keys.Skip((i - 1)*20).Take(20);
                res.SendJson(liked);
            });

            server.Get("/meme/:meme", (req, res) =>
            {
                var memeid = req.Params["meme"];
                long mm = 0;
                if (!long.TryParse(memeid, out mm))
                {
                    res.Redirect("/404");
                    return;
                }
                Meme meme;
                if (!dict.TryGetValue(mm, out meme))
                {
                    res.Redirect("/404");
                    return;
                }
                var rp = new RenderParams
                {
                    {"title", meme.Title},
                    {"score", meme.Score},
                    {"path", meme.WebPath},
                    {"thread", meme.ThreadName},
                    {"threadId", meme.ThreadId},
                    {"orgId", meme.OrgId},
                    {"total", dict.Length}
                };
                res.RenderPage("pages/index.ecs", rp);
            });

            server.Post("/meme/:meme/vote", (req, res) =>
            {
                var pq = req.GetFormData();
                var u = pq["user"];
                var p = pq["pass"];
                var m = req.Params["meme"];
                var v = pq["val"];
                int val;

                if (string.IsNullOrWhiteSpace(u) || string.IsNullOrWhiteSpace(m) || string.IsNullOrWhiteSpace(v) ||
                    string.IsNullOrWhiteSpace(p))
                {
                    res.SendString("no");
                    return;
                }

                if (!int.TryParse(v, out val) || val < -1 || val > 1)
                {
                    res.SendString("no");
                    return;
                }

                long mm = 0;
                if (!long.TryParse(m, out mm))
                {
                    res.SendString("no");
                    return;
                }

                Meme meme;
                if (!dict.TryGetValue(mm, out meme))
                {
                    res.SendString("no");
                    return;
                }

                var user = mdb.Get<User>(u);
                if (user == null || user.PassHash != p)
                {
                    res.SendString("no");
                    return;
                }

                int cv;
                meme.Vote(user.Votes.TryGetValue(m, out cv) ? CalcVote(cv, val) : val);
                user.Votes[m] = val;
                mdb.Update(meme);
                mdb.Update(user);
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

                long mm = 0;
                if (!long.TryParse(m, out mm))
                {
                    res.SendString("no");
                    return;
                }
                Meme meme;
                if (!dict.TryGetValue(mm, out meme))
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
                mdb.Insert(new Report(m, rr, email, reason));
                res.SendString("ok");
            });

            server.Get("/meme/:meme/remove", async (req, res) =>
            {
                var pass = req.Queries["pwd"];
                var m = req.Params["meme"];
                var pwd = File.ReadAllText("rmpwd");
                long mm = 0;
                if (!long.TryParse(m, out mm))
                {
                    res.SendString("no");
                    return;
                }

                Meme meme;
                if (pwd != pass || !dict.TryGetValue(mm, out meme))
                {
                    var r = rand.Next(750, 3000);
                    await Task.Delay(r);
                    res.SendString("no");
                    return;
                }
                await Task.Delay(500);
                mdb.Delete<Meme>(meme.OrgId);
                dict.Remove(meme);
                File.Delete(meme.Path);
                File.Delete(meme.Thumb);
                res.Redirect("/");
            });

            server.Get("/thread/:thread", (req, res) =>
            {
                var tid = req.Params["thread"];
                int t = 0;
                if (!int.TryParse(tid, out t))
                {
                    res.Redirect("/404");
                    return;
                }
                var th = mdb.FindOne<Meme>(m => m.ThreadId == t);
                var memes = mdb.Find<Meme>(m => m.ThreadId == t).Select(m => m.OrgId);

                var pars = new RenderParams
                {
                    {"thread", tid},
                    {"threadname", th.ThreadName},
                    {"memes", memes}
                };
                res.RenderPage("pages/thr/thread.ecs", pars);
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
                var reports = mdb.GetTable<Report>().ToList();
                res.SendJson(reports);
            });

            server.Get("/multimeme", (req, res) =>
            {
                var size = req.Queries["size"];
                if (string.IsNullOrEmpty(size) || !Regex.IsMatch(size, "[0-9]+x[0-9]+", RegexOptions.Compiled))
                    size = "3x3";
                var split = size.Split('x');
                var h = int.Parse(split[0]);
                var w = int.Parse(split[1]);
                if (h > 4) h = 4;
                if (w > 5) w = 5;
                var tot = h*w;
                var limit = tot*3;
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

                var user = mdb.FindOne<User>(u => u.Username == uid);
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
                var un = data["username"];
                var pw = data["password"];
                if (un != null || pw != null)
                {
                    res.SendString("no");
                    return;
                }
                var user = mdb.FindOne<User>(u => u.Username == un);
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
                mdb.Insert(user);
                res.SendString("ok");
            });

            crawler.Start();

            Logger.Configure(LoggingOption.File, true, "LOG.txt");
            server.Start();
        }

        private static MemeDictionary LoadMemes(IEnumerable<Meme> memes)
        {
            var retVal = new MemeDictionary();
            foreach (var meme in memes)
            {
                retVal.Add(meme);
            }
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
}
