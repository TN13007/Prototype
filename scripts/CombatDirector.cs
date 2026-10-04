using Godot;

public partial class CombatDirector : Node
{
    private Scenario[] scenarios;
    private Scenario currentScenario;
    private Batch currentBatch;

    private int currentBatchIndex;
    private float batchTimer;

    private int nextBatchEnemyIndex;

    private float attackInterval = 0.5f;
    private float attackTimer = 0f;
    private int currentSlotIndex = 0;
    private float slotTimer = 0f;

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

        scenarios = new Scenario[]
        {
            new Scenario(
                new Batch[]
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
                    ),
                }
            ),

            new Scenario(
                new Batch[]
                {
                    new Batch(
                        BatchPattern.Single,
                        batchEnemies,
                        1.5f,
                        0.4f
                    ),

                    new Batch(
                        BatchPattern.Single,
                        batchEnemies,
                        3f,
                        0.8f
                    ),
                    new Batch(
                        BatchPattern.Double,
                        batchEnemies,
                        2f,
                        1f
                    )
                }
            ),

            new Scenario(
                new Batch[]
                {
                    new Batch(
                        BatchPattern.Single,
                        batchEnemies,
                        3f,
                        0.3f
                    ),

                    new Batch(
                        BatchPattern.Single,
                        batchEnemies,
                        1.5f,
                        1f
                    )
                }
            )
        };

        currentScenario = scenarios[1];
        int scenarioIndex = 1;
        GD.Print($"DIRECTOR: SCENARIO {scenarioIndex + 1} START");

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
        slotTimer -= delta;

        if (currentSlotIndex < currentBatch.Slots.Length)
        {
            AttackSlot slot = currentBatch.Slots[currentSlotIndex];

            if (slotTimer <= 0f)
            {
                ExecuteSlot(slot);

                currentSlotIndex++;

                if (currentSlotIndex < currentBatch.Slots.Length)
                {
                    AttackSlot nextSlot =
                        currentBatch.Slots[currentSlotIndex];

                    slotTimer =
                        nextSlot.Time - slot.Time;
                }
            }
        }

        if (batchTimer <= 0f)
        {
            GD.Print("DIRECTOR: BATCH END");

            state = DirectorState.Rest;
            restTimer = currentBatch.RestDuration;
        }
    }

    private void ExecuteSlot(AttackSlot slot)
    {
        if (currentBatch == null)
            return;

        if (slot.Pattern == AttackPattern.Single)
        {
            Enemy enemy = FindAvailableEnemy();

            if (enemy == null)
            {
                GD.Print("DIRECTOR: SLOT MISSED");

                return;
            }

            enemy.StartAttack();

            GD.Print(
                $"DIRECTOR: SINGLE → {enemy.Name}"
            );

            return;
        }

        if (slot.Pattern == AttackPattern.Double)
        {
            Enemy first = FindAvailableEnemy();
            Enemy second = FindAvailableEnemy(first);

            if (first == null || second == null)
            {
                GD.Print("DIRECTOR: DOUBLE BROKEN");

                return;
            }

            first.StartAttack();
            second.StartAttack();

            GD.Print(
                $"DIRECTOR: DOUBLE → {first.Name} + {second.Name}"
            );
        }
    }
    private Enemy FindAvailableEnemy()
    {
        foreach (Enemy enemy in currentBatch.Enemies)
        {
            if (enemy == null)
                continue;

            if (!IsInstanceValid(enemy))
                continue;

            if (!enemy.IsIdle)
                continue;

            return enemy;
        }

        return null;
    }
    private Enemy FindAvailableEnemy(Enemy excluded)
    {
        foreach (Enemy enemy in currentBatch.Enemies)
        {
            if (enemy == null)
                continue;

            if (!IsInstanceValid(enemy))
                continue;

            if (enemy == excluded)
                continue;

            if (!enemy.IsIdle)
                continue;

            return enemy;
        }

        return null;
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

        currentSlotIndex = 0;
        slotTimer = 0f;

        state = DirectorState.Batch;

        GD.Print(
            $"DIRECTOR: BATCH {currentBatchIndex + 1} START"
        );
    }
}
