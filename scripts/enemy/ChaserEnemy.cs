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

    [Export]
    private float gravity = 1400f;


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
        ApplyGravity(delta);

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

        MoveAndSlide();
    }


    private void ApplyGravity(float delta)
    {
        if (IsOnFloor())
        {
            if (Velocity.Y > 0f)
            {
                Velocity = new Vector2(
                    Velocity.X,
                    0f
                );
            }

            return;
        }

        Velocity = new Vector2(
            Velocity.X,
            Velocity.Y + gravity * delta
        );
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


        float horizontalDistance =
            player.GlobalPosition.X -
            GlobalPosition.X;

        float absoluteDistance =
            Mathf.Abs(horizontalDistance);


        /*
         * Player quá xa.
         * Chạy nhanh để nhanh chóng tiến vào vùng chiến đấu.
         */
        if (absoluteDistance >
            preferredDistance + runDistance)
        {
            float direction =
                Mathf.Sign(horizontalDistance);

            Velocity = new Vector2(
                direction * runSpeed,
                Velocity.Y
            );

            return;
        }


        /*
         * Player ở xa vùng đứng lý tưởng.
         * Đi bộ để điều chỉnh vị trí.
         */
        if (absoluteDistance >
            preferredDistance + positionTolerance)
        {
            float direction =
                Mathf.Sign(horizontalDistance);

            Velocity = new Vector2(
                direction * walkSpeed,
                Velocity.Y
            );

            return;
        }


        /*
         * Enemy đang quá gần player.
         * Đi bộ lùi để lấy lại khoảng cách.
         */
        if (absoluteDistance <
            preferredDistance - positionTolerance)
        {
            float direction =
                -Mathf.Sign(horizontalDistance);

            Velocity = new Vector2(
                direction * walkSpeed,
                Velocity.Y
            );

            return;
        }


        /*
         * Đã nằm trong vùng khoảng cách lý tưởng.
         * Không di chuyển theo X.
         *
         * Velocity.Y vẫn được giữ lại
         * để gravity tiếp tục hoạt động.
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


        /*
         * Chưa tới thời điểm bắt đầu di chuyển.
         * Đứng yên theo X nhưng vẫn chịu gravity.
         */
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
         * Dừng theo X và chờ đúng beat.
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
         * Combat movement:
         * chỉ chạy theo trục X.
         */
        float direction =
            Mathf.Sign(horizontalDistance);

        Velocity = new Vector2(
            direction * runSpeed,
            Velocity.Y
        );
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

        if (currentThreat != null &&
            currentThreat.Target is Player player)
        {
            bool tookDamage =
                player.TakeDamage(1);

            if (!tookDamage)
            {
                GD.Print(
                    $"{Name}: ATTACK WAS PARRIED"
                );
            }
        }

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
