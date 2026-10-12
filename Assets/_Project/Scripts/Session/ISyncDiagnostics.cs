namespace Fireflies.Session
{
    public interface ISyncDiagnostics
    {
        bool InterpolationEnabled { get; set; }
        int RoundTripMs { get; }
        int BufferedSnapshots { get; }
    }
}
