using SQLite;

namespace MemesterRHttp
{
    [Table("Memes")]
    class Meme
    {
        [PrimaryKey]
        public long OrgId { get; set; }
        public string Title { get; set; }
        public long ThreadId { get; set; }
        public string ThreadName { get; set; }
        public int Score { get; private set; }
        
        private readonly object _lock = new object();

        [Ignore]
        public string Path => System.IO.Path.Combine("public", "memes", $"{OrgId}.webm");
        [Ignore]
        public string WebPath => $"/memes/{OrgId}.webm";
        [Ignore]
        public string Thumb => System.IO.Path.Combine("public", "thumbs", $"{OrgId}.png");
        [Ignore]
        public string WebThumb => $"/thumbs/{OrgId}.png";


        public void Vote(int vote)
        {
            lock (_lock)
            {
                Score += vote;
            }
        }
    }
}