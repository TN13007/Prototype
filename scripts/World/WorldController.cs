using Godot;
using System.Collections.Generic;

public partial class WorldController : Node2D
{
    public List<SpawnSlot> SpawnSlots { get; } = new();

    public override void _Ready()
    {
        FindSpawnSlots();
    }

    private void FindSpawnSlots()
    {
        SpawnSlots.Clear();
        foreach (Node node in GetChildren())
        {
            if (node is SpawnSlot slot)
                SpawnSlots.Add(slot);
        }
    }

    public bool Initialize(PrototypeWorldConfig world)
    {
        if (world == null)
            return false;

        bool allSlotsFound = true;
        foreach (SpawnSlotDefinition definition in world.SpawnSlots)
        {
            SpawnSlot slot = SpawnSlots.Find(candidate => candidate.SlotId == definition.Id);
            if (slot == null)
            {
                GD.PushError($"WORLD: Config references missing spawn slot: {definition.Id}");
                allSlotsFound = false;
                continue;
            }

            slot.Apply(definition);
        }

        if (!allSlotsFound)
            return false;

        GD.Print($"WORLD: Loaded {world.Id} with {SpawnSlots.Count} spawn slots");
        return true;
    }

    public SpawnSlot GetSpawnSlot(EnemyType type)
    {
        foreach (SpawnSlot slot in SpawnSlots)
        {
            if (slot.CanSpawn(type))
                return slot;
        }
        return null;
    }

    /// <summary>
    /// For reserves, prefer the available spawn point farthest from the player so reinforcements enter from a readable distance.
    /// </summary>
    public SpawnSlot GetBestSpawnSlot(EnemyType type, Vector2 targetPosition, bool farthestFromTarget = false)
    {
        SpawnSlot best = null;
        float bestDistance = farthestFromTarget ? float.MinValue : float.MaxValue;

        foreach (SpawnSlot slot in SpawnSlots)
        {
            if (!slot.CanSpawn(type))
                continue;

            float distance = slot.GlobalPosition.DistanceSquaredTo(targetPosition);
            if ((!farthestFromTarget && distance < bestDistance) ||
                (farthestFromTarget && distance > bestDistance))
            {
                best = slot;
                bestDistance = distance;
            }
        }

        return best;
    }
}
