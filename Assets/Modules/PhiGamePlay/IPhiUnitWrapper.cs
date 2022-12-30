using Klrohias.NFast.PhiChartLoader;

namespace Klrohias.NFast.PhiGamePlay
{
    public interface IPhiUnitWrapper
    {
        public void DoEvent(UnitEventType type, float value);
        public PhiUnit Unit { get; set; }
    }
}