using System.Collections.Generic;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;

namespace Klrohias.NFast.ChartLoader.LargePez
{
    public class LargePezChart : IChart
    {
        internal ZipFile zipFile = null;
        internal Dictionary<uint, List<long>> offsetMap = new();
        internal int noteCount = 0;
        internal JsonTokenizer tokenizer = null;
        internal JsonWalker walker = null;
        public IEnumerator<IList<ChartNote>> GetNotes()
        {
            uint beats = 0;
            var processedNotes = 0;
            var lastNotes = new List<ChartNote>();
            while (processedNotes < noteCount)
            {
                if (!offsetMap.ContainsKey(beats)) yield return lastNotes;
                lastNotes.AddRange(offsetMap[beats].Select(x => LargePezLoader.ExtractNote(this, x).ToNFastNote()));
                yield return lastNotes;
            }
        }

        public IEnumerator<IList<ChartLine>> GetLines()
        {
            throw new System.NotImplementedException();
        }
    }
}