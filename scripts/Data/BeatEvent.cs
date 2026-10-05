public class BeatEvent
{
    public float Time;

    public ThreatType ThreatType;

    public float LeadTime;


    public BeatEvent(
        float time,
        ThreatType type,
        float leadTime)
    {
        Time = time;
        ThreatType = type;
        LeadTime = leadTime;
    }
}
