using Klrohias.NFast.PhiChartLoader;

namespace Klrohias.NFast.PhiGamePlay
{
    public interface IPhiNoteWrapper
    {
        public PhiNote Note { get; set; }
        public void NoteStart();
        public bool IsJudged { get; set; }
        public PhiGamePlayer Player { get; set; }
    }
}