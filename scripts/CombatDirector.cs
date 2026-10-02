using Godot;

public partial class CombatDirector : Node
{
    private Batch currentBatch;
    private float batchTimer;

    private int nextBatchEnemyIndex;

    private float attackInterval = 0.5f;
    private float attackTimer = 0f;

    public override void _Ready()
    {
        var enemies = GetTree().GetNodesInGroup("enemy");

        Enemy[] batchEnemies = new Enemy[enemies.Count];

        for (int i = 0; i < enemies.Count; i++)
        {
            batchEnemies[i] = enemies[i] as Enemy;
        }

        currentBatch = new Batch(
            BatchPattern.Single,
            batchEnemies,
            5f,
            0.6f
        );

        batchTimer = currentBatch.Duration;

        GD.Print("DIRECTOR: BATCH START");
    }

    public override void _Process(double delta)
    {
        UpdateBatch((float)delta);
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

        if (batchTimer <= 0f)
        {
            GD.Print("DIRECTOR: BATCH END");

            currentBatch = null;
        }
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
}
