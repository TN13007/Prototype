using Godot;


public enum CombatThreatResult
{
    Completed,
    Interrupted,
    Failed
}


public sealed class CombatThreat
{
    public int Id { get; }

    public CombatBeat Beat { get; }

    public Node2D Target { get; }

    public Enemy AssignedEnemy { get; }


    public CombatThreat(
        int id,
        CombatBeat beat,
        Node2D target,
        Enemy assignedEnemy)
    {
        Id = id;

        Beat = beat;

        Target = target;

        AssignedEnemy = assignedEnemy;
    }
}
