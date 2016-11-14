using SQLite;

namespace MemesterRHttp
{
    [Table("Memes")]
    class Meme
    {
        [PrimaryKey]
        public string OrgId { get; set; }
        public string Title { get; set; }
        public string Ext { get; set; }
        public int Score { get; private set; }

        private readonly object _lock = new object();

        [Ignore]
        public string Path => System.IO.Path.Combine("public", "memes", $"{OrgId}.{Ext}");


        public void Vote(int vote)
        {
            lock (_lock)
            {
                Score += vote;
            }
        }
    }
}