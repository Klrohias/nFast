using System.Collections.Generic;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;
using Klrohias.NFast.Json;
using Klrohias.NFast.PhiChartLoader.NFast;

namespace Klrohias.NFast.PhiChartLoader.LargePez
{
    public class LargePezChart : IPhiChart
    {
        internal ZipFile zipFile = null;
        internal Dictionary<string, ZipEntry> files = null;

        internal Dictionary<uint, List<long>> offsetMap = new();
        internal int noteCount = 0;
        internal JsonTokenizer tokenizer = null;
        internal JsonWalker walker = null;
        internal ChartMetadata metadata;
        public ChartMetadata Metadata => metadata;
        public IList<ChartNote> GetNotes()
        {
            return null;
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