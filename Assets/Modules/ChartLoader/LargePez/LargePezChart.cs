using System.Collections.Generic;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;
using Klrohias.NFast.ChartLoader.NFast;

namespace Klrohias.NFast.ChartLoader.LargePez
{
    public class LargePezChart : IChart
    {
        internal ZipFile zipFile = null;
        internal Dictionary<string, ZipEntry> files = null;

        internal Dictionary<uint, List<long>> offsetMap = new();
        internal int noteCount = 0;
        internal JsonTokenizer tokenizer = null;
        internal JsonWalker walker = null;
        internal ChartMetadata metadata;
        public ChartMetadata Metadata => metadata;
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

        public IEnumerator<IList<LineEvent>> GetEvents()
        {
            throw new System.NotImplementedException();
        }


        public IList<ChartLine> GetLines()
        {
            throw new System.NotImplementedException();
        }

        public IEnumerator<KeyValuePair<ChartTimespan, float>> GetBpmEvents()
        {
            throw new System.NotImplementedException();
        }

        public IList<KeyValuePair<ChartTimespan, float>> BpmEvents { get; }
    }
}