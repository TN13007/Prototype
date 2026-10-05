public enum BatchPattern
{
    Single,
    Double
}


public sealed class Batch
{
    public BatchPattern Pattern { get; }

    public CombatBeat[] Beats { get; }

    public float Duration { get; }

    public float RestDuration { get; }


    public Batch(
        BatchPattern pattern,
        float duration,
        float restDuration,
        params CombatBeat[] beats)
    {
        Pattern = pattern;

        Duration = duration;

        RestDuration = restDuration;

        Beats = beats;
    }
}
