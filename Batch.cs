using Godot;

public enum BatchPattern
{
    Single,
    Double,
    Continuous
}

public class Batch
{
    public BatchPattern Pattern { get; }
    public Enemy[] Enemies { get; }
    public float Duration { get; }
    public float RestDuration { get; }

    public Batch(
        BatchPattern pattern,
        Enemy[] enemies,
        float duration,
        float restDuration)
    {
        Pattern = pattern;
        Enemies = enemies;
        Duration = duration;
        RestDuration = restDuration;
    }
}
