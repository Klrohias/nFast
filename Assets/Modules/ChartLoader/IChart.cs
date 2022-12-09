using System.Collections.Generic;

namespace Klrohias.NFast.ChartLoader
{
    public interface IChart
    {
        public IEnumerator<IList<ChartNote>> GetNotes();
        public IEnumerator<IList<LineEvent>> GetEvents();
    }
}