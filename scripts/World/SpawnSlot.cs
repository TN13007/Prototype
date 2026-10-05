using Godot;


public partial class SpawnSlot : Marker2D
{
    [Export]
    public string SlotId { get; set; }

    [Export]
    public Godot.Collections.Array<EnemyType> AllowedEnemies { get; set; } = new();

    public bool IsOccupied { get; private set; }


    public bool CanSpawn(EnemyType type)
    {
        return !IsOccupied && AllowedEnemies.Contains(type);
    }


    public void Apply(SpawnSlotDefinition definition)
    {
        SlotId = definition.Id;
        Position = new Vector2(definition.PositionX, definition.PositionY);
        AllowedEnemies = new Godot.Collections.Array<EnemyType>(definition.AllowedEnemies.ToArray());
    }


    public void SetOccupied(bool occupied)
    {
        IsOccupied = occupied;
    }
}
