using Klrohias.NFast.PhiChartLoader.NFast;

namespace Klrohias.NFast.PhiGamePlay
{
    public interface IPhiNoteWrapper
    {
        public void NoteStart(ChartNote note);
        public bool IsJudged { get; set; }
    }
}