using System.Collections.Generic;
using LiteDB;

namespace MemesterCore
{
    class User
    {
        [BsonId]
        public string Username { get; set; }
        public string PassHash { get; set; }

        public Dictionary<string, int> Votes { get; } = new Dictionary<string, int>();
    }
}