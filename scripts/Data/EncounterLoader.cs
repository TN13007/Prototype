using System.Linq;


public class EncounterLoader
{
    public EncounterTimeline Compose(
        EncounterConfig encounter,
        MusicPhraseConfig phrase)
    {
        EncounterTimeline timeline = new();

        if(encounter == null || phrase == null || phrase.Bpm <= 0f ||
           phrase.BeatsPerBar <= 0)
        {
            return timeline;
        }

        float secondsPerBeat = 60f / phrase.Bpm;

        foreach(EncounterSectionDefinition section in
            encounter.Sections.OrderBy(value => value.StartBar))
        {
            if(section.BeatsBetweenThreats <= 0)
                continue;

            int firstBeat = section.StartBar * phrase.BeatsPerBar;
            int endBeat = (section.StartBar + section.BarCount) *
                          phrase.BeatsPerBar;

            for(int beat = firstBeat;
                beat < endBeat;
                beat += section.BeatsBetweenThreats)
            {
                timeline.Add(new BeatEvent(
                    beat * secondsPerBeat,
                    section.ThreatType,
                    section.LeadTime
                ));
            }
        }

        return timeline;
    }
}
