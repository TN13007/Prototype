using System.Collections.Generic;


public class EncounterTimeline
{
    public List<BeatEvent> Events;


    public EncounterTimeline()
    {
        Events = new List<BeatEvent>();
    }


    public void Add(
        BeatEvent beat)
    {
        Events.Add(beat);
    }
}
