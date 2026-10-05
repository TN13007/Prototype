using Godot;
using System.Collections.Generic;


public partial class EnemyManager : Node
{
    private List<Enemy> enemies =
        new();

    private CombatClock clock;

    private WorldController world;

    private EnemyRosterConfig roster;

    private readonly Dictionary<EnemyType, int> remainingReserve = new();



    public override void _Ready()
    {
        AddToGroup(
            "enemy_manager");

        clock =
            GetTree()
            .GetFirstNodeInGroup("combat_clock")
            as CombatClock;
    }



    public void Register(
        Enemy enemy)
    {
        if(!enemies.Contains(enemy))
        {
            enemies.Add(enemy);


            GD.Print(
                $"MANAGER: Registered {enemy.Name}");
        }
    }


    public void Initialize(
        WorldController worldController,
        EnemyRosterConfig enemyRoster)
    {
        world = worldController;
        roster = enemyRoster;
        remainingReserve.Clear();

        if(roster == null)
            return;

        foreach(EnemyRosterEntry entry in roster.Entries)
            remainingReserve[entry.EnemyType] = entry.ReserveCount;
    }



    public void Unregister(
        Enemy enemy)
    {
        enemies.Remove(enemy);
    }



    private static bool IsUsableEnemy(
        Enemy enemy)
    {
        if(enemy == null)
            return false;

        return IsInstanceValid(enemy);
    }


    private void PruneInvalid()
    {
        for(int i = enemies.Count - 1;
            i >= 0;
            i--)
        {
            if(!IsUsableEnemy(enemies[i]))
                enemies.RemoveAt(i);
        }
    }


    public Enemy FindBestCandidate(
        ThreatRequest request)
    {
        if(clock == null)
            return null;

        PruneInvalid();

        Enemy bestCandidate = null;
        float bestScore = float.MinValue;

        foreach(Enemy enemy in enemies)
        {
            if(!IsUsableEnemy(enemy))
                continue;

            if(!enemy.TryGetCandidateScore(
                request,
                clock,
                out float score))
                continue;

            if(score > bestScore)
            {
                bestCandidate = enemy;
                bestScore = score;
            }
        }

        Enemy spawned = TrySpawnReserve(request);

        if(spawned != null && spawned.TryGetCandidateScore(
            request,
            clock,
            out float spawnedScore))
        {
            return spawned;
        }

        return bestCandidate;
    }


    private Enemy TrySpawnReserve(ThreatRequest request)
    {
        if(request == null || roster == null || world == null)
            return null;

        foreach(EnemyRosterEntry entry in roster.Entries)
        {
            if(!remainingReserve.TryGetValue(entry.EnemyType, out int reserve) ||
               reserve <= 0 ||
               !CanHandle(entry.EnemyType, request.Type) ||
               CountActive(entry.EnemyType) >= entry.MaxActive)
            {
                continue;
            }

            Vector2 target = request.Target == null
                ? Vector2.Zero
                : request.Target.GlobalPosition;

            SpawnSlot slot = world.GetBestSpawnSlot(entry.EnemyType, target);

            if(slot == null)
                continue;

            PackedScene scene = ResourceLoader.Load<PackedScene>(entry.ScenePath);

            if(scene == null)
            {
                GD.PushError($"Could not load reserve enemy scene: {entry.ScenePath}");
                continue;
            }

            Enemy enemy = scene.Instantiate<Enemy>();
            enemy.GlobalPosition = slot.GlobalPosition;
            AddChild(enemy);
            slot.SetOccupied(true);
            enemy.TreeExited += () => slot.SetOccupied(false);
            remainingReserve[entry.EnemyType]--;

            GD.Print($"MANAGER: Spawned reserve {entry.EnemyType} at {slot.SlotId}");
            return enemy;
        }

        return null;
    }


    private int CountActive(EnemyType type)
    {
        int count = 0;

        foreach(Enemy enemy in enemies)
        {
            if(enemy != null && IsInstanceValid(enemy) &&
               enemy.EnemyTypeId == type)
            {
                count++;
            }
        }

        return count;
    }


    private static bool CanHandle(
        EnemyType enemyType,
        ThreatType threatType)
    {
        return enemyType == EnemyType.Chaser &&
               threatType == ThreatType.Melee;
    }


    public bool TryReserve(
        Enemy enemy,
        ThreatRequest request)
    {
        if(enemy == null || !enemy.TryReserveThreat(request))
            return false;

        if(request.Reserve(enemy))
            return true;

        enemy.ReleaseReservation(request);
        return false;
    }



    public override void _Process(
        double delta)
    {
        PruneInvalid();
    }
}
