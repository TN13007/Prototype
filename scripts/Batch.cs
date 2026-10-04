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

    public AttackSlot[] Slots { get; }

    public Batch(
        BatchPattern pattern,
        Enemy[] enemies,
        float duration,
        float restDuration
    )
    {
        Pattern = pattern;
        Enemies = enemies;
        Duration = duration;
        RestDuration = restDuration;

        Slots = CreateSlots(pattern, duration);
    }

    private AttackSlot[] CreateSlots(
        BatchPattern pattern,
        float duration
    )
    {
        if (pattern == BatchPattern.Double)
        {
            return new AttackSlot[]
            {
                new AttackSlot(AttackPattern.Double, 0f),
                new AttackSlot(AttackPattern.Double, duration * 0.5f)
            };
        }

        float interval = 0.6f;

        int count = Mathf.Max(
            1,
            Mathf.FloorToInt(duration / interval)
        );

        AttackSlot[] slots = new AttackSlot[count];

        for (int i = 0; i < count; i++)
        {
            slots[i] = new AttackSlot(
                AttackPattern.Single,
                i * interval
            );
        }

        return slots;
    }
}
