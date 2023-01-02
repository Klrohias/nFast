namespace Klrohias.NFast.Time
{
    public interface IClock
    {
        public float Time { get; }
        public void Reset();
        public void Pause();
        public void Resume();
    }
}