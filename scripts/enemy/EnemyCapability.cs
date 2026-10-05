public class EnemyCapability
{
    public ThreatType Type;


    public float PreparationTime;


    public float AttackRange;

    public float ApproachSpeed;


    public EnemyCapability(
        ThreatType type,
        float preparationTime,
        float attackRange,
        float approachSpeed)
    {
        Type = type;
        PreparationTime = preparationTime;
        AttackRange = attackRange;
        ApproachSpeed = approachSpeed;
    }
}
