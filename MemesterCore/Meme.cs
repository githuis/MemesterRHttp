using System;
using LiteDB;

namespace MemesterCore
{
    class Meme
    {
        public Meme()
        {
            Downloaded = DateTime.UtcNow;
        }

        [BsonId]
        public long OrgId { get; set; }
        public string Title { get; set; }
        public long ThreadId { get; set; }
        public string ThreadName { get; set; }
        public int Score { get; private set; }
        public DateTime Downloaded { get; set; }
        

        [BsonIgnore]
        public string Path => System.IO.Path.Combine("public", "memes", $"{OrgId}.webm");
        [BsonIgnore]
        public string WebPath => $"/memes/{OrgId}.webm";
        [BsonIgnore]
        public string Thumb => System.IO.Path.Combine("public", "thumbs", $"{OrgId}.jpg");
        [BsonIgnore]
        public string WebThumb => $"/thumbs/{OrgId}.jpg";


        public void Vote(int vote)
        {
            Score += vote;
        }
    }
}