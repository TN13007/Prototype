using Godot;

public partial class CombatDirector : Node
{
    private Scenario currentScenario;
    private Batch currentBatch;

    private int currentBatchIndex;
    private float batchTimer;

    private int nextBatchEnemyIndex;

    private float attackInterval = 0.5f;
    private float attackTimer = 0f;

    private enum DirectorState
    {
        Batch,
        Rest,
        Finished
    }

    private DirectorState state = DirectorState.Batch;
    private float restTimer;

    public override void _Ready()
    {
        var enemies = GetTree().GetNodesInGroup("enemy");

        Enemy[] batchEnemies = new Enemy[enemies.Count];

        for (int i = 0; i < enemies.Count; i++)
        {
            batchEnemies[i] = enemies[i] as Enemy;
        }

        Batch[] batches =
        {
            new Batch(
                BatchPattern.Single,
                batchEnemies,
                2f,
                0.6f
            ),

            new Batch(
                BatchPattern.Single,
                batchEnemies,
                2f,
                0.6f
            )
        };

        currentScenario = new Scenario(batches);

        currentBatchIndex = 0;
        currentBatch = currentScenario.Batches[currentBatchIndex];

        batchTimer = currentBatch.Duration;

        GD.Print("DIRECTOR: BATCH START");
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        switch (state)
        {
            case DirectorState.Batch:
                UpdateBatch(dt);
                break;

            case DirectorState.Rest:
                UpdateRest(dt);
                break;

            case DirectorState.Finished:
                break;
        }
    }

    private void UpdateBatch(float delta)
    {
        if (currentBatch == null)
            return;

        batchTimer -= delta;
        attackTimer -= delta;

        if (attackTimer <= 0f)
        {
            StartNextBatchAttack();

            attackTimer = attackInterval;
        }

        if (batchTimer > 0f)
            return;

        GD.Print("DIRECTOR: BATCH END");

        state = DirectorState.Rest;
        restTimer = currentBatch.RestDuration;
    }

    private void StartNextBatchAttack()
    {
        if (currentBatch == null)
            return;

        int count = currentBatch.Enemies.Length;

        if (count == 0)
            return;

        for (int i = 0; i < count; i++)
        {
            int index = (nextBatchEnemyIndex + i) % count;

            Enemy enemy = currentBatch.Enemies[index];

            if (enemy == null)
                continue;

            if (!enemy.IsIdle)
                continue;

            enemy.StartAttack();

            nextBatchEnemyIndex = (index + 1) % count;

            GD.Print(
                $"DIRECTOR: BATCH ATTACK → {enemy.Name}"
            );

            return;
        }
    }

    private void UpdateRest(float delta)
    {
        restTimer -= delta;

        if (restTimer > 0f)
            return;

        GD.Print("DIRECTOR: REST END");

        currentBatchIndex++;

        if (currentBatchIndex >= currentScenario.Batches.Length)
        {
            currentBatch = null;
            state = DirectorState.Finished;

            GD.Print("DIRECTOR: ENCOUNTER END");

            return;
        }

        currentBatch = currentScenario.Batches[currentBatchIndex];

        batchTimer = currentBatch.Duration;
        nextBatchEnemyIndex = 0;
        attackTimer = 0f;

        state = DirectorState.Batch;

        GD.Print(
            $"DIRECTOR: BATCH {currentBatchIndex + 1} START"
        );
    }
}
