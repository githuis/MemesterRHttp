using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MemesterRHttp
{
    class MemeDictionary
    {
        private readonly ConcurrentDictionary<long, Meme> _dict = new ConcurrentDictionary<long, Meme>();
        private readonly List<Meme> _list = new List<Meme>();
        private readonly HashSet<long> _threads = new HashSet<long>();
        private readonly object _lock = new object();

        public void Add(Meme meme)
        {
            _dict.TryAdd(meme.OrgId, meme);
            _list.Add(meme);
            lock (_lock)
            {
                _threads.Add(meme.ThreadId);
            }
        }
        
        public void Remove(Meme meme)
        {
            Meme m;
            _dict.TryRemove(meme.OrgId, out m);
            _list.Remove(meme);
            lock (_lock)
            {
                _threads.Remove(meme.ThreadId);
            }
        }

        public List<long> GetThreads()
        {
            return _threads.ToList();
        }

        public int Length => _list.Count;

        public bool TryGetValue(long id, out Meme meme)
        {
            return _dict.TryGetValue(id, out meme);
        }

        public Meme this[int index] => _list[index];

        public bool Contains(CMeme m) => _dict.ContainsKey(m.OrgId);
    }
}