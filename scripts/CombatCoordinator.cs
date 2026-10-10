using Godot;
using System.Collections.Generic;

/// <summary>
/// Coordinates who may begin an attack turn. Enemies still own their movement,
/// attack choice, combo and recovery logic; this node only grants/releases turns.
/// </summary>
public partial class CombatCoordinator : Node
{
    [Export] public float MinimumGapBetweenTurns { get; set; } = 0.12f;
    [Export] public float TelegraphLeadTime { get; set; } = 0.45f;
    [Export] public float BeatSubdivision { get; set; } = 0.5f;

    private readonly Dictionary<Enemy, float> waitingEnemies = new();

    private CombatClock clock;
    private EnemyManager enemyManager;
    private Player player;
    private Enemy activeAttacker;
    private Enemy lastAttacker;

    private float secondsPerBeat = 60f / 128f;
    private float lastTurnEndedAt = -1000f;
    private bool combatEnabled;

    public Enemy ActiveAttacker => activeAttacker;
    public bool CombatEnabled => combatEnabled;

    public override void _Ready()
    {
        AddToGroup("combat_coordinator");
        ResolvePlayer();
    }

    public void Initialize(EnemyManager manager, CombatClock combatClock, float bpm)
    {
        enemyManager = manager;
        clock = combatClock;
        ResolvePlayer();

        if (enemyManager == null || clock == null || player == null)
        {
            GD.PushError(
                "COORDINATOR: Cannot initialize. " +
                $"manager={enemyManager != null}, clock={clock != null}, player={player != null}");
            combatEnabled = false;
            return;
        }

        if (bpm > 0f)
            secondsPerBeat = 60f / bpm;

        waitingEnemies.Clear();
        activeAttacker = null;
        lastAttacker = null;
        lastTurnEndedAt = clock.Time - MinimumGapBetweenTurns;
        combatEnabled = true;

        GD.Print(
            $"COORDINATOR: Ready at {bpm:0.##} BPM; " +
            $"one active attack turn, grid={BeatSubdivision:0.##} beat");
    }

    public override void _Process(double delta)
    {
        PruneWaitingQueue();

        if (!combatEnabled || clock == null || player == null || player.IsDead)
            return;

        if (activeAttacker != null && IsInstanceValid(activeAttacker))
            return;

        activeAttacker = null;

        if (player.IsHitstunned)
            return;

        if (clock.Time - lastTurnEndedAt < MinimumGapBetweenTurns)
            return;

        Enemy next = ChooseNextEnemy();
        if (next == null)
            return;

        waitingEnemies.Remove(next);
        activeAttacker = next;

        float impactTime = GetNextImpactTime(TelegraphLeadTime);
        if (!next.BeginAttackTurn(impactTime))
        {
            GD.Print($"COORDINATOR: {next.Name} no longer eligible; releasing turn");
            activeAttacker = null;
            lastTurnEndedAt = clock.Time;
            return;
        }

        GD.Print($"COORDINATOR: Granted attack turn to {next.Name}; first impact @ {impactTime:0.000}");
    }

    public void RequestAttack(Enemy enemy)
    {
        if (!combatEnabled || enemy == null || !IsInstanceValid(enemy) || !enemy.CanReceiveAttackTurn)
            return;

        if (activeAttacker == enemy || waitingEnemies.ContainsKey(enemy))
            return;

        waitingEnemies[enemy] = clock != null ? clock.Time : 0f;
        GD.Print($"COORDINATOR: {enemy.Name} requested an attack turn ({waitingEnemies.Count} waiting)");
    }

    public void CancelRequest(Enemy enemy)
    {
        if (enemy == null)
            return;

        waitingEnemies.Remove(enemy);

        if (activeAttacker == enemy)
            ReleaseTurn(enemy);
    }

    public void CancelEnemy(Enemy enemy)
    {
        if (enemy == null)
            return;

        waitingEnemies.Remove(enemy);

        if (activeAttacker == enemy)
        {
            activeAttacker = null;
            lastAttacker = enemy;
            lastTurnEndedAt = clock != null ? clock.Time : lastTurnEndedAt;
            GD.Print($"COORDINATOR: {enemy.Name} lost its turn");
        }
    }

    public void ReleaseTurn(Enemy enemy)
    {
        if (enemy == null)
            return;

        waitingEnemies.Remove(enemy);

        if (activeAttacker != enemy)
            return;

        activeAttacker = null;
        lastAttacker = enemy;
        lastTurnEndedAt = clock != null ? clock.Time : lastTurnEndedAt;
        GD.Print($"COORDINATOR: {enemy.Name} released its attack turn");
    }

    /// <summary>Returns the next beat-grid point at least minimumLeadSeconds in the future.</summary>
    public float GetNextImpactTime(float minimumLeadSeconds)
    {
        if (clock == null || secondsPerBeat <= 0f)
            return 0f;

        float gridSize = secondsPerBeat * Mathf.Max(BeatSubdivision, 0.125f);
        float earliest = clock.Time + Mathf.Max(minimumLeadSeconds, 0.05f);
        return Mathf.Ceil(earliest / gridSize) * gridSize;
    }

    public void StopCombat()
    {
        combatEnabled = false;
        waitingEnemies.Clear();
        activeAttacker = null;
        GD.Print("COORDINATOR: Combat stopped");
    }

    private Enemy ChooseNextEnemy()
    {
        Enemy best = null;
        float bestScore = float.MinValue;

        foreach (KeyValuePair<Enemy, float> entry in waitingEnemies)
        {
            Enemy enemy = entry.Key;
            if (enemy == null || !IsInstanceValid(enemy) || !enemy.CanReceiveAttackTurn)
                continue;

            float waitAge = Mathf.Max(0f, clock.Time - entry.Value);
            float distance = enemy.GlobalPosition.DistanceTo(player.GlobalPosition);
            float recentAttackerPenalty = enemy == lastAttacker ? 1.5f : 0f;

            // Waiting time dominates, while proximity and turn rotation break ties.
            float score = waitAge * 10f - distance * 0.0015f - recentAttackerPenalty;
            if (score > bestScore)
            {
                bestScore = score;
                best = enemy;
            }
        }

        return best;
    }

    private void PruneWaitingQueue()
    {
        List<Enemy> remove = new();
        foreach (KeyValuePair<Enemy, float> entry in waitingEnemies)
        {
            Enemy enemy = entry.Key;
            if (enemy == null || !IsInstanceValid(enemy) || !enemy.CanReceiveAttackTurn)
                remove.Add(enemy);
        }

        foreach (Enemy enemy in remove)
            waitingEnemies.Remove(enemy);

        if (activeAttacker != null && !IsInstanceValid(activeAttacker))
        {
            activeAttacker = null;
            lastTurnEndedAt = clock != null ? clock.Time : lastTurnEndedAt;
        }
    }

    private void ResolvePlayer()
    {
        player = GetTree().GetFirstNodeInGroup("player") as Player;
    }
}
