﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RHttpServer;
using RHttpServer.Plugins.External;
using RHttpServer.Response;

namespace MemesterRHttp
{
    class Program
    {
        private static readonly object _reportLock = new object();

        static void Main(string[] args)
        {
            var server = new HttpServer(5000, 3, "./public") { CachePublicFiles = true };
            var db = new SimpleSQLiteDatatase("memes.db");
            var dict = LoadMemes(db.GetTable<Meme>());

            var crawler = new Crawler(dict, db, TimeSpan.FromMinutes(2));

            var rand = new Random();

            server.Get("/", (req, res) =>
            {
                var m = rand.Next(0, dict.Count);
                var meme = dict.ElementAt(m).Value;
                res.Redirect("/meme/" + meme.OrgId);
            });

            server.Get("/instameme", (req, res) =>
            {
                var m = rand.Next(0, dict.Count);
                var meme = dict.ElementAt(m).Value;
                res.Redirect("/" + meme.Path);
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
                    {"path", meme.Path}
                };
                res.RenderPage("/pages/singlememe.ecs", rp);
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
                var rn = req.Queries["rn"];
                var reason = req.Queries["reason"];
                var uid = req.Queries["uid"];

                if (string.IsNullOrWhiteSpace(rn) || (rn == "4" && string.IsNullOrWhiteSpace(reason)))
                {
                    res.SendString("no");
                    return;
                }

                if (string.IsNullOrWhiteSpace(uid)) uid = "$ANON";

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
                
                // will do for now
                lock (_reportLock)
                {
                    File.AppendAllText("reported.txt", $"{m}\t{uid}{rn}\t{reason}\n");
                }
                res.SendString("ok");
            });

            server.Post("/meme/:meme/remove", async (req, res) =>
            {
                var pass = req.Queries["pass"];
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
                dict.TryRemove(m, out meme);
                File.Delete(meme.Path);
                File.Delete(meme.Thumb);
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
                var l = dict.Count;
                if (limit > l) limit = l;
                var r = new Random();
                var list = new List<Meme>();
                for (int i = 0; i < limit; i++)
                {
                    list.Add(dict.ElementAt(r.Next(0, l)).Value);
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
                var uid = login["uid"];
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
                res.RenderPage("/pages/registerpage.ecs", null);
            });

            server.Post("/register", (req, res) =>
            {
                var data = req.GetBodyPostFormData();
                if (!data.ContainsKey("username") || !data.ContainsKey("passhash"))
                {
                    res.SendString("no");
                    return;
                }
                var user = db.Get<User>(data["username"]);
                if (user != null)
                {
                    res.SendString("no");
                    return;
                }
                user = new User
                {
                    Username = data["username"],
                    PassHash = data["passhash"]
                };
                db.Insert(user);
                res.SendString("ok");
            });

            crawler.Start();
            
        }

        private static ConcurrentDictionary<string, Meme> LoadMemes(IEnumerable<Meme> memes)
        {
            var retVal = new ConcurrentDictionary<string, Meme>();
            foreach (var meme in memes)
            {
                retVal.TryAdd(meme.OrgId, meme);
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

    enum ReportReason
    {
        NSFW,
        NotFunny,
        Abuse,
        Other
    }
}
