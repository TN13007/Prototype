using Godot;


public class SpawnSlotData
{
    public Vector2 Position;


    public EnemyType[] Allowed;


    public SpawnSlotData(
        Vector2 position,
        EnemyType[] allowed)
    {
        Position = position;
        Allowed = allowed;
    }
}
