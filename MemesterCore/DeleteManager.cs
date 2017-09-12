using System.IO;
using System.Linq;
using LiteDB;

namespace MemesterCore
{
    class DeleteManager
    {
        private const double AvgSize = 3.5;
        private readonly MemeDictionary _memedict;
        private readonly LiteCollection<Meme> _memedb;

        public int MaxMemes { get; set; }

        public DeleteManager(MemeDictionary mdict, LiteCollection<Meme> mdb, int maxSizeGb)
        {
            _memedict = mdict;
            _memedb = mdb;
            MaxMemes = (int)((maxSizeGb * 1000) / AvgSize);
        }

        public void MakeRoom(int newmemes)
        { 
            var overflow = newmemes + _memedict.Length - MaxMemes;
            if (overflow > 0)
            {
                var dd = _memedict.OrderByDescending(m => m.Downloaded).Take(overflow);
                foreach (var meme in dd)
                {
                    _memedict.Remove(meme);
                    _memedb.Delete(meme.OrgId);
                    File.Delete(meme.Path);
                    File.Delete(meme.Thumb);
                }
            }
        }
    }
}