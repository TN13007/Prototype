using Godot;

public partial class CombatDirector : Node
{
    [Export]
    public float StartDelay = 1f;

    [Export]
    public float CheckInterval = 0.2f;

    private float startTimer;
    private float checkTimer;

    public override void _Ready()
    {
        startTimer = StartDelay;
        checkTimer = 0f;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        if (startTimer > 0f)
        {
            startTimer -= dt;
            return;
        }

        checkTimer -= dt;

        if (checkTimer > 0f)
            return;

        checkTimer = CheckInterval;

        TryStartEnemyAttack();
    }

    private void TryStartEnemyAttack()
    {
        Godot.Collections.Array<Node> enemies =
            GetTree().GetNodesInGroup("enemy");

        if (enemies.Count == 0)
            return;

        // Không cho enemy mới attack nếu vẫn còn
        // enemy đang trong Telegraph / Attack / Recovery.
        foreach (Node node in enemies)
        {
            if (node is Enemy enemy && !enemy.IsIdle)
                return;
        }

        foreach (Node node in enemies)
        {
            if (node is Enemy enemy && enemy.IsIdle)
            {
                enemy.StartAttack();

                GD.Print("DIRECTOR: START ENEMY ATTACK");

                return;
            }
        }
    }
}
