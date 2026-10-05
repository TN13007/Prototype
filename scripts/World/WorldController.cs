using Godot;
using System.Collections.Generic;


public partial class WorldController : Node2D
{
    public List<SpawnSlot> SpawnSlots { get; } =
        new();



    public override void _Ready()
    {
        FindSpawnSlots();
    }



    private void FindSpawnSlots()
    {
        foreach(Node node in GetChildren())
        {
            if(node is SpawnSlot slot)
            {
                SpawnSlots.Add(slot);
            }
        }
    }


    public bool Initialize(PrototypeWorldConfig world)
    {
        if(world == null)
            return false;

        foreach(SpawnSlotDefinition definition in world.SpawnSlots)
        {
            SpawnSlot slot = SpawnSlots.Find(candidate =>
                candidate.SlotId == definition.Id);

            if(slot == null)
            {
                GD.PushError($"World config references missing slot: {definition.Id}");
                continue;
            }

            slot.Apply(definition);
        }

        GD.Print($"WORLD: Loaded {world.Id} with {SpawnSlots.Count} spawn slots");
        return true;
    }



    public SpawnSlot GetSpawnSlot(
        EnemyType type)
    {
        foreach(SpawnSlot slot in SpawnSlots)
        {
            foreach(
                EnemyType allowed
                in slot.AllowedEnemies)
                if(allowed == type && !slot.IsOccupied)
                    return slot;
        }


        return null;
    }


    public SpawnSlot GetBestSpawnSlot(
        EnemyType type,
        Vector2 targetPosition)
    {
        SpawnSlot best = null;
        float bestDistance = float.MaxValue;

        foreach(SpawnSlot slot in SpawnSlots)
        {
            if(!slot.CanSpawn(type))
                continue;

            float distance = slot.GlobalPosition.DistanceSquaredTo(targetPosition);

            if(distance < bestDistance)
            {
                best = slot;
                bestDistance = distance;
            }
        }

        return best;
    }
}
