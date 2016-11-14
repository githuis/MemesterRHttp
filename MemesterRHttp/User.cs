using System.Collections.Generic;
using SQLite;

namespace MemesterRHttp
{
    [Table("Users")]
    class User
    {
        [PrimaryKey]
        public string Username { get; set; }
        public string PassHash { get; set; }

        public Dictionary<string, int> Votes { get; } = new Dictionary<string, int>();
    }
}