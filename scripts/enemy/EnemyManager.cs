using Godot;
using System.Collections.Generic;

public partial class EnemyManager : Node
{
    [Export] public float ReserveSpawnDelay { get; set; } = 0.85f;

    private readonly List<Enemy> enemies = new();
    private readonly Dictionary<EnemyType, int> remainingReserve = new();
    private readonly List<EnemyType> pendingSpawns = new();

    private WorldController world;
    private EnemyRosterConfig roster;
    private float spawnTimer;
    private int nextReserveInstanceId = 1;

    public int ActiveEnemyCount
    {
        get
        {
            PruneInvalidEnemies();
            int count = 0;
            foreach (Enemy enemy in enemies)
            {
                if (enemy != null && IsInstanceValid(enemy) && enemy.State != EnemyState.Dead)
                    count++;
            }
            return count;
        }
    }

    public int RemainingReserveCount
    {
        get
        {
            int count = 0;
            foreach (int amount in remainingReserve.Values)
                count += amount;
            return count;
        }
    }

    public int PendingSpawnCount => pendingSpawns.Count;

    public override void _Ready()
    {
        AddToGroup("enemy_manager");
    }

    public void Initialize(WorldController worldController, EnemyRosterConfig enemyRoster)
    {
        world = worldController;
        roster = enemyRoster;
        remainingReserve.Clear();
        pendingSpawns.Clear();
        spawnTimer = ReserveSpawnDelay;

        if (roster == null || roster.Entries == null)
        {
            GD.PushError("MANAGER: Enemy roster is missing or invalid");
            return;
        }

        foreach (EnemyRosterEntry entry in roster.Entries)
        {
            if (entry == null)
                continue;

            remainingReserve[entry.EnemyType] = Mathf.Max(0, entry.ReserveCount);
        }

        GD.Print($"MANAGER: Initialized; reserve count={RemainingReserveCount}");
    }

    public void Register(Enemy enemy)
    {
        if (enemy == null || enemies.Contains(enemy))
            return;

        enemies.Add(enemy);
        enemy.Defeated += OnEnemyDefeated;
        GD.Print($"MANAGER: Registered {enemy.Name}");
    }

    public void Unregister(Enemy enemy)
    {
        if (enemy == null)
            return;

        enemy.Defeated -= OnEnemyDefeated;
        enemies.Remove(enemy);
    }

    public override void _Process(double delta)
    {
        PruneInvalidEnemies();

        if (pendingSpawns.Count == 0)
            return;

        spawnTimer -= (float)delta;
        if (spawnTimer > 0f)
            return;

        EnemyType type = pendingSpawns[0];
        if (TrySpawnReserve(type))
        {
            pendingSpawns.RemoveAt(0);
            spawnTimer = ReserveSpawnDelay;
        }
        else
        {
            // Retry when a slot becomes available or its scene finishes loading.
            spawnTimer = 0.40f;
        }
    }

    private void OnEnemyDefeated(Enemy enemy)
    {
        if (enemy == null)
            return;

        GD.Print($"MANAGER: {enemy.Name} defeated; checking reserves");
        QueueReserveSpawn(enemy.EnemyTypeId);
    }

    private void QueueReserveSpawn(EnemyType type)
    {
        EnemyRosterEntry entry = FindRosterEntry(type);
        if (entry == null)
            return;

        int remaining = remainingReserve.TryGetValue(type, out int value) ? value : 0;
        int alreadyPending = CountPending(type);
        int active = CountActive(type);

        if (remaining <= alreadyPending || active + alreadyPending >= entry.MaxActive)
            return;

        pendingSpawns.Add(type);
        if (pendingSpawns.Count == 1)
            spawnTimer = ReserveSpawnDelay;

        GD.Print($"MANAGER: Queued reserve {type}; pending={pendingSpawns.Count}");
    }

    private bool TrySpawnReserve(EnemyType type)
    {
        if (world == null || roster == null)
            return false;

        EnemyRosterEntry entry = FindRosterEntry(type);
        if (entry == null)
            return false;

        Player player = GetTree().GetFirstNodeInGroup("player") as Player;
        Vector2 targetPosition = player != null ? player.GlobalPosition : Vector2.Zero;
        SpawnSlot slot = world.GetBestSpawnSlot(type, targetPosition, true);
        if (slot == null)
        {
            GD.PushWarning($"MANAGER: No free spawn slot for reserve {type}");
            return false;
        }

        PackedScene scene = ResourceLoader.Load<PackedScene>(entry.ScenePath);
        if (scene == null)
        {
            GD.PushError($"MANAGER: Could not load enemy scene: {entry.ScenePath}");
            return false;
        }

        Enemy enemy = scene.Instantiate<Enemy>();
        if (enemy == null)
        {
            GD.PushError($"MANAGER: Scene root is not an Enemy: {entry.ScenePath}");
            return false;
        }

        enemy.Name = $"{type}_Reserve_{nextReserveInstanceId++}";
        slot.SetOccupied(true);
        AddChild(enemy);
        enemy.GlobalPosition = slot.GlobalPosition;
        enemy.TreeExited += () => slot.SetOccupied(false);

        int remaining = remainingReserve.TryGetValue(type, out int count) ? count : 0;
        remainingReserve[type] = Mathf.Max(0, remaining - 1);

        GD.Print($"MANAGER: Spawned reserve {enemy.Name} at {slot.SlotId}; reserves left={RemainingReserveCount}");
        return true;
    }

    private int CountActive(EnemyType type)
    {
        int count = 0;
        foreach (Enemy enemy in enemies)
        {
            if (enemy != null && IsInstanceValid(enemy) &&
                enemy.State != EnemyState.Dead && enemy.EnemyTypeId == type)
            {
                count++;
            }
        }
        return count;
    }

    private int CountPending(EnemyType type)
    {
        int count = 0;
        foreach (EnemyType pending in pendingSpawns)
        {
            if (pending == type)
                count++;
        }
        return count;
    }

    private EnemyRosterEntry FindRosterEntry(EnemyType type)
    {
        if (roster?.Entries == null)
            return null;

        foreach (EnemyRosterEntry entry in roster.Entries)
        {
            if (entry != null && entry.EnemyType == type)
                return entry;
        }
        return null;
    }

    private void PruneInvalidEnemies()
    {
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            if (enemies[i] == null || !IsInstanceValid(enemies[i]))
                enemies.RemoveAt(i);
        }
    }
}
