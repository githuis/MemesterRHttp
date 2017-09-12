using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MemesterCore
{
    class MemeDictionary : IEnumerable<Meme>
    {
        private readonly ConcurrentDictionary<long, Meme> _dict = new ConcurrentDictionary<long, Meme>();
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

        public bool TryGetValue(long id, out Meme meme)
        {
            return _dict.TryGetValue(id, out meme);
        }

        public Meme this[int index] => _list[index];

        public bool Contains(CMeme m) => _dict.ContainsKey(m.OrgId);

        public IEnumerator<Meme> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}