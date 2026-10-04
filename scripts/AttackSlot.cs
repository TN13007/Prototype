public enum AttackPattern
{
    Single,
    Double
}

public class AttackSlot
{
    public AttackPattern Pattern { get; }
    public float Time { get; }
    public int RequiredEnemies { get; }

    public AttackSlot(
        AttackPattern pattern,
        float time
    )
    {
        Pattern = pattern;
        Time = time;

        RequiredEnemies = pattern switch
        {
            AttackPattern.Single => 1,
            AttackPattern.Double => 2,
            _ => 1
        };
    }
}
