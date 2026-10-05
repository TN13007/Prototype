using Godot;


public class EnemyPlanner
{
    public float CalculateStartTime(
        Enemy enemy,
        ThreatRequest threat,
        float moveSpeed)
    {
        if(enemy == null ||
           threat == null ||
           moveSpeed <= 0f)
        {
            return float.PositiveInfinity;
        }

        EnemyCapability capability =
            enemy.GetCapability(
                threat.Type);

        if(capability == null)
            return float.PositiveInfinity;

        float approachDistance =
            enemy.GetApproachDistance(
                threat,
                capability);

        float travelTime =
            approachDistance /
            moveSpeed;

        return
            threat.ImpactTime -
            travelTime -
            capability.PreparationTime;
    }
}
