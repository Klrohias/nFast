using System.Collections.Generic;
using System.Threading.Tasks;
using Klrohias.NFast.PhiChartLoader.NFast;

namespace Klrohias.NFast.PhiChartLoader
{
    public interface IPhiChart
    {
        public ChartMetadata Metadata { get; }
        public IList<ChartNote> GetNotes();
        public IEnumerator<IList<LineEvent>> GetEvents();
        public IList<ChartLine> GetLines();
        public IEnumerator<KeyValuePair<ChartTimespan, float>> GetBpmEvents();
        public IList<ChartNote> GetNotesByBeatIndex(int index);
    }
}