using System.Collections.Generic;
using Klrohias.NFast.ChartLoader.NFast;

namespace Klrohias.NFast.ChartLoader
{
    public interface IChart
    {
        public ChartMetadata Metadata { get; }
        public IEnumerator<IList<ChartNote>> GetNotes();
        public IEnumerator<IList<LineEvent>> GetEvents();
        public IList<ChartLine> GetLines();
        public IEnumerator<KeyValuePair<ChartTimespan, float>> GetBpmEvents();
    }
}