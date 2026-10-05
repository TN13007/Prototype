using Godot;

public partial class ChaserEnemy : Enemy
{
    [ExportGroup("Movement")]
    [Export]
    private float runSpeed = 300f;
    [Export]
    private float walkSpeed = 110f;
    [Export]
    private float preferredDistance = 80f;
    [Export]
    private float positionTolerance = 12f;
    [Export]
    private float runDistance = 220f;
    private float attackStartTime;
    private CombatClock clock;
    private EnemyPlanner planner =
        new EnemyPlanner();
    private Node2D player;

    protected override void RegisterCapabilities()
    {
        capabilities.Add(
            new EnemyCapability(
                ThreatType.Melee,
                0.5f,
                preferredDistance,
                runSpeed
            )
        );
    }

    public override void _Ready()
    {
        base._Ready();

        clock =
            GetTree()
            .GetFirstNodeInGroup("combat_clock")
            as CombatClock;

        player =
            GetTree()
            .GetFirstNodeInGroup("player")
            as Node2D;
    }

    protected override void UpdateEnemy(float delta)
    {
        switch (state)
        {
            case EnemyState.Idle:
                MaintainPosition();
                break;

            case EnemyState.Planning:
                Plan();
                break;

            case EnemyState.Moving:
                MoveToTarget();
                break;

            case EnemyState.Telegraph:
                Telegraph();
                break;

            case EnemyState.Executing:
                ExecuteAttack();
                break;

            case EnemyState.Recovery:
                Recover();
                break;
        }
    }

    private void MaintainPosition()
    {
        if (player == null)
        {
            Velocity = new Vector2(
                0f,
                Velocity.Y
            );

            return;
        }

        float distance =
            player.GlobalPosition.X -
            GlobalPosition.X;

        float absoluteDistance =
            Mathf.Abs(distance);

        /*
         * Player đang ở rất xa.
         * Chạy nhanh để rút ngắn khoảng cách.
         */
        if (absoluteDistance >
            preferredDistance + runDistance)
        {
            float direction =
                Mathf.Sign(distance);

            Velocity = new Vector2(
                direction * runSpeed,
                Velocity.Y
            );
            return;
        }

        /*
         * Player ở khoảng vừa.
         * Đi bộ để điều chỉnh vị trí.
         */
        if (absoluteDistance >
            preferredDistance + positionTolerance)
        {
            float direction =
                Mathf.Sign(distance);

            Velocity = new Vector2(
                direction * walkSpeed,
                Velocity.Y
            );
            return;
        }

        /*
         * Player áp sát quá gần.
         * Đi bộ lùi ra để giữ khoảng đánh.
         */
        if (absoluteDistance <
            preferredDistance - positionTolerance)
        {
            float direction =
                -Mathf.Sign(distance);

            Velocity = new Vector2(
                direction * walkSpeed,
                Velocity.Y
            );
            return;
        }

        /*
         * Đã ở đúng khoảng.
         */
        Velocity = new Vector2(
            0f,
            Velocity.Y
        );
    }

    private void Plan()
    {
        if (clock == null ||
            currentThreat == null)
        {
            ResolveCurrentThreat(
                ThreatState.Failed,
                "Combat clock or threat is unavailable"
            );
            ChangeState(
                EnemyState.Idle
            );
            return;
        }

        attackStartTime =
            planner.CalculateStartTime(
                this,
                currentThreat,
                runSpeed
            );

        if (attackStartTime >
            currentThreat.ImpactTime)
        {
            ResolveCurrentThreat(
                ThreatState.Failed,
                "Cannot reach target before impact"
            );
            ChangeState(
                EnemyState.Idle
            );
            return;
        }
        ChangeState(
            EnemyState.Moving
        );
        GD.Print(
            $"{Name}: attack starts at {attackStartTime}"
        );
    }

    private void MoveToTarget()
    {
        if (currentThreat == null ||
            currentThreat.Target == null)
        {
            ChangeState(
                EnemyState.Idle
            );
            return;
        }

        if (clock == null)
        {
            ResolveCurrentThreat(
                ThreatState.Failed,
                "Combat clock is unavailable"
            );
            ChangeState(
                EnemyState.Idle
            );
            return;
        }

        if (clock.Time < attackStartTime)
        {
            Velocity = new Vector2(
                0f,
                Velocity.Y
            );
            return;
        }

        float horizontalDistance =
            currentThreat.Target.GlobalPosition.X -
            GlobalPosition.X;

        float absoluteDistance =
            Mathf.Abs(horizontalDistance);

        EnemyCapability capability =
            GetCapability(
                currentThreat.Type
            );

        float remainingTime =
            currentThreat.ImpactTime -
            clock.Time;

        if (remainingTime < -0.1f)
        {
            ResolveCurrentThreat(
                ThreatState.Failed,
                "Missed impact time"
            );
            ChangeState(
                EnemyState.Idle
            );
            return;
        }

        /*
         * Đã tới khoảng đánh.
         * Chờ đúng beat để Telegraph.
         */
        if (absoluteDistance <=
            capability.AttackRange &&
            remainingTime <=
            capability.PreparationTime)
        {
            Velocity = new Vector2(
                0f,
                Velocity.Y
            );
            ChangeState(
                EnemyState.Telegraph
            );
            return;
        }

        /*
         * Chỉ chạy theo trục X.
         */
        float direction =
            Mathf.Sign(horizontalDistance);

        Velocity = new Vector2(
            direction * runSpeed,
            Velocity.Y
        );

        MoveAndSlide();
    }

    private void Telegraph()
    {
        Velocity = new Vector2(
            0f,
            Velocity.Y
        );

        if (clock == null ||
            currentThreat == null)
        {
            ResolveCurrentThreat(
                ThreatState.Failed,
                "Combat clock or threat is unavailable"
            );
            ChangeState(
                EnemyState.Idle
            );
            return;
        }

        if (clock.Time <
            currentThreat.ImpactTime)
        {
            return;
        }

        GD.Print(
            $"{Name}: TELEGRAPH"
        );

        ChangeState(
            EnemyState.Executing
        );
    }

    private void ExecuteAttack()
    {
        Velocity = new Vector2(
            0f,
            Velocity.Y
        );

        GD.Print(
            $"{Name}: ATTACK IMPACT"
        );

        ResolveCurrentThreat(
            ThreatState.Completed,
            "Attack reached impact time"
        );

        ChangeState(
            EnemyState.Recovery
        );
    }

    private void Recover()
    {
        Velocity = new Vector2(
            0f,
            Velocity.Y
        );

        GD.Print(
            $"{Name}: Recovery"
        );

        ChangeState(
            EnemyState.Idle
        );
    }
}
