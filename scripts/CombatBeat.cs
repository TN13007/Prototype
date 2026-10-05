public enum CombatBeatType
{
    MeleeSlash
}

public enum EnemyRole
{
    Any,
    Melee,
    Ranged
}


public sealed class CombatBeat
{
    public float Time { get; }

    public CombatBeatType Type { get; }

    public EnemyRole RequiredRole { get; }

    public int GroupId { get; }


    public CombatBeat(
        float time,
        CombatBeatType type,
        EnemyRole requiredRole,
        int groupId)
    {
        Time = time;

        Type = type;

        RequiredRole = requiredRole;

        GroupId = groupId;
    }
}
