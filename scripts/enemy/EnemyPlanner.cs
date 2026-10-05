using Godot;


public class EnemyPlanner
{
    public float CalculateStartTime(
        Enemy enemy,
        ThreatRequest threat,
        float moveSpeed)
    {
        if (threat == null ||
            threat.Target == null)
            return
                float.PositiveInfinity;

        if (moveSpeed <= 0f)
            return
                float.PositiveInfinity;

        float distance =
            enemy.GlobalPosition
            .DistanceTo(
                threat.Target.GlobalPosition);


        float moveTime =
            distance / moveSpeed;


        EnemyCapability capability =
            enemy.GetCapability(
                threat.Type);


        float prepare =
            capability.PreparationTime;


        return
            threat.ImpactTime
            -
            moveTime
            -
            prepare;
    }
}
