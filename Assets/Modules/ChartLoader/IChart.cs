using System.Collections.Generic;

namespace Klrohias.NFast.ChartLoader
{
    public interface IChart
    {
        public ChartMetadata Metadata { get; }
        public IEnumerator<IList<ChartNote>> GetNotes();
        public IEnumerator<IList<ChartLine>> GetLines();
        public IList<KeyValuePair<ChartTimespan, float>> BpmEvents { get; }
    }
}