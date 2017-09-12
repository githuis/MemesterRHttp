using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LiteDB;
using RedHttpServerCore;
using RedHttpServerCore.Plugins;
using RedHttpServerCore.Plugins.Interfaces;
using RedHttpServerCore.Response;

namespace MemesterCore
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new RedHttpServer(3000);
            var mdb = new LiteDatabase("memes.ldb").GetCollection<Meme>("Memes");
            var udb = new LiteDatabase("users.ldb").GetCollection<User>("Users");
            var rdb = new LiteDatabase("report.ldb").GetCollection<Report>("Reports");

            
            var dict = LoadMemes(mdb.FindAll());
            var delMan = new DeleteManager(dict, mdb, 12);
            var crawler = new Crawler(mdb, delMan, dict, TimeSpan.FromMinutes(5));
            var rand = new Random();

            server.Get("/", async (req, res) =>
            {
                var m = rand.Next(0, dict.Length - 1);
                var meme = dict[m];
                await res.Redirect("/meme/" + meme.OrgId);
            });


            server.Get("/404", async (req, res) =>
            {
                await res.RenderPage("pages/404.ecs", null);
            });
            
            server.Get("/user/:user", async (req, res) =>
            {
                var par = new RenderParams
                {
                    {"total", dict.Length}
                };
                await res.RenderPage("pages/acc/account.ecs", par);
            });

            server.Get("/user/:user/liked", async (req, res) =>
            {
                var user = req.Params["user"];
                var items = req.Queries["p"];
                if (string.IsNullOrEmpty(items)) items = "1";
                var u = udb.FindById(user);
                int i;
                if (u == null || !int.TryParse(items, out i))
                {
                    await res.SendString("no");
                    return;
                }

                var liked = u.Votes.Keys.Skip((i - 1) * 20).Take(20);
                await res.SendJson(liked);
            });

            server.Get("/meme/:meme", async (req, res) =>
            {
                var memeid = req.Params["meme"];
                long mm = 0;
                if (!long.TryParse(memeid, out mm))
                {
                    await res.Redirect("/404");
                    return;
                }
                Meme meme;
                if (!dict.TryGetValue(mm, out meme))
                {
                    await res.Redirect("/404");
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
                await res.RenderPage("pages/index.ecs", rp);
            });

            server.Post("/meme/:meme/vote", async (req, res) =>
            {
                var pq = await req.GetFormDataAsync();
                var u = pq["user"][0];
                var p = pq["pass"][0];
                var m = req.Params["meme"];
                var v = pq["val"][0];
                int val;

                if (string.IsNullOrWhiteSpace(u) || string.IsNullOrWhiteSpace(m) || string.IsNullOrWhiteSpace(v) ||
                    string.IsNullOrWhiteSpace(p))
                {
                    await res.SendString("no");
                    return;
                }

                if (!int.TryParse(v, out val) || val < -1 || val > 1)
                {
                    await res.SendString("no");
                    return;
                }

                long mm = 0;
                if (!long.TryParse(m, out mm))
                {
                    await res.SendString("no");
                    return;
                }

                Meme meme;
                if (!dict.TryGetValue(mm, out meme))
                {
                    await res.SendString("no");
                    return;
                }

                var user = udb.FindById(u);
                if (user == null || user.PassHash != p)
                {
                    await res.SendString("no");
                    return;
                }

                int cv;
                meme.Vote(user.Votes.TryGetValue(m, out cv) ? CalcVote(cv, val) : val);
                user.Votes[m] = val;
                mdb.Update(meme);
                udb.Update(user);
                await res.SendString("ok");
            });

            server.Post("/meme/:meme/report", async (req, res) =>
            {
                var m = req.Params["meme"];
                var body = await req.GetFormDataAsync();
                var rn = body["rn"][0];
                var reason = body["reason"][0];
                var email = body["email"][0];

                if (string.IsNullOrWhiteSpace(rn) || ((rn == "2" || rn == "4") && string.IsNullOrWhiteSpace(reason)))
                {
                    await res.SendString("no");
                    return;
                }

                if (string.IsNullOrWhiteSpace(email)) email = "$ANON";

                int rval;
                if (!int.TryParse(rn, out rval) || rval < 0 || rval > 4)
                {
                    await res.SendString("no");
                    return;
                }

                long mm = 0;
                if (!long.TryParse(m, out mm))
                {
                    await res.SendString("no");
                    return;
                }
                Meme meme;
                if (!dict.TryGetValue(mm, out meme))
                {
                    await res.SendString("no");
                    return;
                }
                var rr = (Report.ReportReason)rval;
                rdb.Insert(new Report(m, rr, email, reason));
                await res.SendString("ok");
            });

            server.Get("/meme/:meme/remove", async (req, res) =>
            {
                var pass = req.Queries["pwd"];
                var m = req.Params["meme"];
                var pwd = File.ReadAllText("rmpwd");
                long mm = 0;
                if (!long.TryParse(m, out mm))
                {
                    await res.SendString("no");
                    return;
                }

                Meme meme;
                if (pwd != pass || !dict.TryGetValue(mm, out meme))
                {
                    var r = rand.Next(750, 3000);
                    await Task.Delay(r);
                    await res.SendString("no");
                    return;
                }
                await Task.Delay(500);
                mdb.Delete(meme.OrgId);
                dict.Remove(meme);
                File.Delete(meme.Path);
                File.Delete(meme.Thumb);
                await res.Redirect("/");
            });

            server.Get("/thread/:thread", async (req, res) =>
            {
                var tid = req.Params["thread"];
                int t = 0;
                if (!int.TryParse(tid, out t))
                {
                    await res.Redirect("/404");
                    return;
                }
                var th = mdb.FindOne(m => m.ThreadId == t);
                var memes = mdb.Find(m => m.ThreadId == t).Select(m => m.OrgId);

                var pars = new RenderParams
                {
                    {"thread", tid},
                    {"threadname", th.ThreadName},
                    {"memes", memes}
                };
                await res.RenderPage("pages/thr/thread.ecs", pars);
            });

            server.Post("/reportedmemes", async (req, res) =>
            {
                var body = await req.GetFormDataAsync();
                var pass = body["pwd"][0];
                var pwd = File.ReadAllText("rmpwd");
                if (pass != pwd)
                {
                    var r = rand.Next(750, 1500);
                    await Task.Delay(r);
                    await res.SendString("no");
                    return;
                }
                var reports = mdb.FindAll().ToList();
                await res.SendJson(reports);
            });

            server.Get("/multimeme", async (req, res) =>
            {
                var size = req.Queries["size"][0];
                if (string.IsNullOrEmpty(size) || !Regex.IsMatch(size, "[0-9]+x[0-9]+", RegexOptions.Compiled))
                    size = "3x3";
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

                await res.RenderPage("./pages/multimeme.ecs", new RenderParams
                {
                    {"h", h},
                    {"w", w},
                    {"m", list}
                });
            });

            server.Post("/login", async (req, res) =>
            {
                var login = await req.GetFormDataAsync();
                var uid = login["usr"];
                var pwd = login["pwd"];
                if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(pwd))
                {
                    res.SendString("no");
                    return;
                }

                var user = udb.FindOne(u => u.Username == uid);
                if (user == null || user.PassHash != pwd)
                {
                    await res.SendString("no");
                    return;
                }
                await res.SendString("ok");
            });

            server.Get("/register", async (req, res) =>
            {
                await res.RenderPage("pages/reg/register.ecs", null);
            });

            server.Post("/register", async (req, res) =>
            {
                var data = await req.GetFormDataAsync();
                var un = data["username"][0];
                var pw = data["password"][0];
                if (un != null || pw != null)
                {
                    await res.SendString("no");
                    return;
                }
                var user = udb.FindOne(u => u.Username == un);
                if (user != null)
                {
                    await res.SendString("no");
                    return;
                }
                user = new User
                {
                    Username = un,
                    PassHash = data["password"][0]
                };
                udb.Insert(user);
                await res.SendString("ok");
            });

            server.Get("/*", async (req, res) =>
            {
                await res.Redirect("/404");
            });
            crawler.Start();
            server.Plugins.Register<ILogging, FileLogging>(new FileLogging("LOG"));
            server.Start();
            Console.ReadKey();
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