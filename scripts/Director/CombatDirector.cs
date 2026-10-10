using Godot;

/// <summary>
/// Encounter lifecycle only. It does not schedule attacks; CombatCoordinator does that at runtime.
/// </summary>
public partial class CombatDirector : Node
{
    private EnemyManager enemyManager;
    private CombatCoordinator coordinator;
    private CombatClock clock;
    private Player player;
    private EncounterState state = EncounterState.Finished;
    private bool reportedWaitForClear;

    public void Initialize(
        EnemyManager manager,
        CombatCoordinator combatCoordinator,
        CombatClock combatClock,
        Player controlledPlayer)
    {
        enemyManager = manager;
        coordinator = combatCoordinator;
        clock = combatClock;
        player = controlledPlayer;

        if (enemyManager == null || coordinator == null || clock == null || player == null)
        {
            GD.PushError(
                "DIRECTOR: Cannot start encounter. " +
                $"manager={enemyManager != null}, coordinator={coordinator != null}, " +
                $"clock={clock != null}, player={player != null}");
            state = EncounterState.Finished;
            return;
        }

        reportedWaitForClear = false;
        state = EncounterState.Running;
        GD.Print(
            $"DIRECTOR: ENCOUNTER STARTED; active={enemyManager.ActiveEnemyCount}, " +
            $"reserves={enemyManager.RemainingReserveCount}");
    }

    public override void _Process(double delta)
    {
        if (state != EncounterState.Running)
            return;

        if (player == null || !IsInstanceValid(player) || player.IsDead)
        {
            Finish(false);
            return;
        }

        if (enemyManager == null || !IsInstanceValid(enemyManager))
            return;

        if (enemyManager.ActiveEnemyCount == 0 &&
            enemyManager.RemainingReserveCount == 0 &&
            enemyManager.PendingSpawnCount == 0)
        {
            Finish(true);
            return;
        }

        if (!reportedWaitForClear &&
            enemyManager.ActiveEnemyCount == 0 &&
            (enemyManager.RemainingReserveCount > 0 || enemyManager.PendingSpawnCount > 0))
        {
            reportedWaitForClear = true;
            GD.Print("DIRECTOR: No active enemies; waiting for the final reserve spawn");
        }

        if (enemyManager.ActiveEnemyCount > 0)
            reportedWaitForClear = false;
    }

    private void Finish(bool victory)
    {
        if (state == EncounterState.Finished)
            return;

        state = EncounterState.Finished;
        coordinator?.StopCombat();
        clock?.Stop();
        GD.Print(victory ? "DIRECTOR: ENCOUNTER CLEAR" : "DIRECTOR: PLAYER DEFEATED");
    }
}
