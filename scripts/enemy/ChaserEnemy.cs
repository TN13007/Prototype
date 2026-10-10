using Godot;

public partial class ChaserEnemy : Enemy
{
    [ExportGroup("Movement")]
    [Export] private float runSpeed = 300f;
    [Export] private float walkSpeed = 110f;
    [Export] private float attackRange = 66f;
    [Export] private float runDistance = 220f;
    [Export] private float gravity = 1400f;

    [ExportGroup("Attack")]
    [Export] private float attackActiveDuration = 0.10f;
    [Export] private float recoveryDuration = 0.18f;
    [Export] private float comboPauseDuration = 0.08f;
    [Export] private float comboTelegraphLeadTime = 0.30f;
    [Export] private int minimumComboHits = 1;
    [Export] private int maximumComboHits = 2;

    private CombatClock clock;
    private AudioStreamPlayer2D telegraphSound;
    private Polygon2D bodyVisual;
    private Color normalColor = Colors.White;

    private float impactTime;
    private float stateTimer;
    private float facingDirection = 1f;
    private int comboHitCount;
    private int completedComboHits;
    private bool recoveringBetweenComboHits;

    public override bool CanReceiveAttackTurn
    {
        get
        {
            if (state != EnemyState.WaitingForTurn || player == null || player.IsDead)
                return false;

            return Mathf.Abs(player.GlobalPosition.X - GlobalPosition.X) <= attackRange + 8f;
        }
    }

    public override void _Ready()
    {
        base._Ready();

        clock = GetTree().GetFirstNodeInGroup("combat_clock") as CombatClock;
        telegraphSound = GetNodeOrNull<AudioStreamPlayer2D>("TelegraphSound");
        bodyVisual = GetNodeOrNull<Polygon2D>("Polygon2D");
        if (bodyVisual != null)
            normalColor = bodyVisual.Modulate;
    }

    protected override void UpdateEnemy(float delta)
    {
        ApplyGravity(delta);

        if (player == null || player.IsDead)
        {
            Velocity = new Vector2(0f, Velocity.Y);
            EndAttackHitbox();
            return;
        }

        switch (state)
        {
            case EnemyState.Approaching:
                ApproachPlayer();
                break;

            case EnemyState.WaitingForTurn:
                WaitForTurn();
                break;

            case EnemyState.Telegraph:
                Velocity = new Vector2(0f, Velocity.Y);
                UpdateTelegraph();
                break;

            case EnemyState.Attacking:
                Velocity = new Vector2(0f, Velocity.Y);
                UpdateAttack(delta);
                break;

            case EnemyState.Recovery:
                Velocity = new Vector2(0f, Velocity.Y);
                UpdateRecovery(delta);
                break;

            case EnemyState.Stunned:
                Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0f, 900f * delta), Velocity.Y);
                break;

            case EnemyState.Dead:
                Velocity = Vector2.Zero;
                break;
        }
    }

    protected override void OnAttackTurnGranted(float firstImpactTime)
    {
        minimumComboHits = Mathf.Max(1, minimumComboHits);
        maximumComboHits = Mathf.Max(minimumComboHits, maximumComboHits);

        comboHitCount = (int)GD.RandRange(minimumComboHits, maximumComboHits);
        completedComboHits = 0;
        recoveringBetweenComboHits = false;
        ScheduleTelegraph(firstImpactTime);
    }

    protected override void OnStunStarted()
    {
        RestoreVisual();
        Velocity = new Vector2(0f, Velocity.Y);
    }

    protected override void OnStunEnded()
    {
        RestoreVisual();
    }

    protected override void OnDeathStarted()
    {
        RestoreVisual();
        Velocity = Vector2.Zero;
    }

    public override void _Draw()
    {
        if (state != EnemyState.Telegraph)
            return;

        // Telegraph direction: a bright line points toward the intended strike lane.
        DrawLine(
            Vector2.Zero,
            new Vector2(facingDirection * 66f, 0f),
            new Color(1f, 0.20f, 0.16f, 0.95f),
            4f,
            true);

        DrawCircle(
            new Vector2(facingDirection * 66f, 0f),
            5f,
            new Color(1f, 0.32f, 0.20f, 1f));
    }

    private void ApproachPlayer()
    {
        float horizontalDistance = player.GlobalPosition.X - GlobalPosition.X;
        float absoluteDistance = Mathf.Abs(horizontalDistance);

        if (absoluteDistance <= attackRange)
        {
            Velocity = new Vector2(0f, Velocity.Y);
            RequestAttackTurn();
            return;
        }

        facingDirection = Mathf.Sign(horizontalDistance);
        if (Mathf.IsZeroApprox(facingDirection))
            facingDirection = 1f;

        float speed = absoluteDistance > runDistance ? runSpeed : walkSpeed;
        Velocity = new Vector2(facingDirection * speed, Velocity.Y);
    }

    private void WaitForTurn()
    {
        float horizontalDistance = player.GlobalPosition.X - GlobalPosition.X;
        float absoluteDistance = Mathf.Abs(horizontalDistance);

        if (absoluteDistance > attackRange + 8f)
        {
            CancelAttackRequest();
            ApproachPlayer();
            return;
        }

        Velocity = new Vector2(0f, Velocity.Y);
        facingDirection = Mathf.Sign(horizontalDistance);
        if (Mathf.IsZeroApprox(facingDirection))
            facingDirection = 1f;
    }

    private void ScheduleTelegraph(float targetImpactTime)
    {
        if (state == EnemyState.Dead || player == null || player.IsDead)
        {
            ReleaseAttackTurn();
            return;
        }

        float horizontalDistance = player.GlobalPosition.X - GlobalPosition.X;
        facingDirection = Mathf.Sign(horizontalDistance);
        if (Mathf.IsZeroApprox(facingDirection))
            facingDirection = 1f;

        impactTime = targetImpactTime;
        stateTimer = 0f;
        ChangeState(EnemyState.Telegraph);

        if (bodyVisual != null)
            bodyVisual.Modulate = new Color(1f, 0.28f, 0.24f, 1f);

        if (telegraphSound != null)
            telegraphSound.Play();

        QueueRedraw();
        GD.Print($"{Name}: TELEGRAPH; impact @ {impactTime:0.000}");
    }

    private void UpdateTelegraph()
    {
        if (clock == null)
        {
            EndAttackTurnAsFailed("Combat clock unavailable");
            return;
        }

        if (clock.Time < impactTime)
            return;

        RestoreVisual();
        ChangeState(EnemyState.Attacking);
        stateTimer = attackActiveDuration;
        BeginAttackHitbox(facingDirection);
        GD.Print($"{Name}: ATTACK IMPACT");
    }

    private void UpdateAttack(float delta)
    {
        stateTimer -= delta;
        if (stateTimer > 0f)
            return;

        EndAttackHitbox();
        completedComboHits++;

        if (!ContactedPlayerThisStrike)
            GD.Print($"{Name}: ATTACK MISSED");

        recoveringBetweenComboHits = completedComboHits < comboHitCount;
        stateTimer = recoveringBetweenComboHits ? comboPauseDuration : recoveryDuration;
        ChangeState(EnemyState.Recovery);
    }

    private void UpdateRecovery(float delta)
    {
        stateTimer -= delta;
        if (stateTimer > 0f)
            return;

        if (recoveringBetweenComboHits)
        {
            float nextImpact = coordinator != null
                ? coordinator.GetNextImpactTime(comboTelegraphLeadTime)
                : (clock != null ? clock.Time + comboTelegraphLeadTime : 0f);

            ScheduleTelegraph(nextImpact);
            return;
        }

        RestoreVisual();
        ChangeState(EnemyState.Approaching);
        ReleaseAttackTurn();
    }

    private void EndAttackTurnAsFailed(string reason)
    {
        EndAttackHitbox();
        RestoreVisual();
        GD.PushWarning($"{Name}: attack turn aborted: {reason}");
        ChangeState(EnemyState.Approaching);
        ReleaseAttackTurn();
    }

    private void RestoreVisual()
    {
        if (bodyVisual != null)
            bodyVisual.Modulate = normalColor;

        QueueRedraw();
    }

    private void ApplyGravity(float delta)
    {
        if (IsOnFloor())
        {
            if (Velocity.Y > 0f)
                Velocity = new Vector2(Velocity.X, 0f);
            return;
        }

        Velocity = new Vector2(Velocity.X, Velocity.Y + gravity * delta);
    }
}
