using Klrohias.NFast.PhiChartLoader;

namespace Klrohias.NFast.PhiGamePlay
{
    public interface IPhiNoteWrapper
    {
        public void NoteStart(PhiNote note);
        public bool IsJudged { get; set; }
    }
}