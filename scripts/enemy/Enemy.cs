using Godot;

public partial class Enemy : CharacterBody2D
{
    private enum EnemyState
    {
        Idle,
        Telegraph,
        Attack,
        Recovery,
        Dead
    }

    [Export] private int maxHp = 3;
    [Export] private float telegraphDuration = 0.5f;
    [Export] private float attackDuration = 0.2f;
    [Export] private float recoveryDuration = 0.5f;
    [Export] private float attackSpeed = 500f;
    [Export] private float attackRange = 30f;

    private int hp;

    private EnemyState state = EnemyState.Idle;
    private float stateTimer;

    private Vector2 attackDirection = Vector2.Right;

    private Node2D player;
    private Area2D attackHitbox;
    private AudioStreamPlayer2D telegraphSound;

    public bool IsIdle => state == EnemyState.Idle;
    public bool IsDead => state == EnemyState.Dead;

    public override void _Ready()
    {
        hp = maxHp;

        player = GetTree().GetFirstNodeInGroup("player") as Node2D;

        attackHitbox = GetNode<Area2D>("AttackHitbox");
        telegraphSound = GetNode<AudioStreamPlayer2D>("TelegraphSound");

        attackHitbox.AreaEntered += OnAttackHitboxAreaEntered;
        attackHitbox.Monitoring = false;

        GD.Print($"Enemy ready. Player found: {player != null}");
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        switch (state)
        {
            case EnemyState.Idle:
                UpdateIdle();
                break;

            case EnemyState.Telegraph:
                UpdateTelegraph(dt);
                break;

            case EnemyState.Attack:
                UpdateAttack(dt);
                break;

            case EnemyState.Recovery:
                UpdateRecovery(dt);
                break;

            case EnemyState.Dead:
                UpdateDead();
                break;
        }

        QueueRedraw();
    }

    // --------------------------------------------------
    // IDLE
    // --------------------------------------------------

    private void UpdateIdle()
    {
        Velocity = Vector2.Zero;

        if (player == null)
        {
            player = GetTree().GetFirstNodeInGroup("player") as Node2D;
        }
    }

    // --------------------------------------------------
    // TELEGRAPH
    // --------------------------------------------------

    private void UpdateTelegraph(float delta)
    {
        Velocity = Vector2.Zero;

        stateTimer -= delta;

        if (stateTimer <= 0f)
        {
            BeginAttack();
        }
    }

    // --------------------------------------------------
    // ATTACK
    // --------------------------------------------------

    private void BeginAttack()
    {
        state = EnemyState.Attack;
        stateTimer = attackDuration;

        attackHitbox.Position = attackDirection * attackRange;
        attackHitbox.Monitoring = true;

        GD.Print("Enemy: ATTACK");
    }

    private void UpdateAttack(float delta)
    {
        Velocity = attackDirection * attackSpeed;

        MoveAndSlide();

        stateTimer -= delta;

        if (stateTimer <= 0f)
        {
            EndAttack();
        }
    }

    private void EndAttack()
    {
        Velocity = Vector2.Zero;

        attackHitbox.Monitoring = false;
        attackHitbox.Position = Vector2.Zero;

        state = EnemyState.Recovery;
        stateTimer = recoveryDuration;

        GD.Print("Enemy: RECOVERY");
    }

    // --------------------------------------------------
    // RECOVERY
    // --------------------------------------------------

    private void UpdateRecovery(float delta)
    {
        Velocity = Vector2.Zero;

        stateTimer -= delta;

        if (stateTimer <= 0f)
        {
            state = EnemyState.Idle;

            GD.Print("Enemy: IDLE");
        }
    }

    // --------------------------------------------------
    // START ATTACK
    // --------------------------------------------------

    public void StartAttack()
    {
        if (state != EnemyState.Idle)
        {
            GD.Print($"Enemy cannot attack. Current state: {state}");
            return;
        }

        if (player == null || !IsInstanceValid(player))
        {
            player = GetTree().GetFirstNodeInGroup("player") as Node2D;
        }

        if (player == null)
        {
            GD.PrintErr("Enemy cannot attack: Player not found.");
            return;
        }

        CalculateAttackDirection();

        state = EnemyState.Telegraph;
        stateTimer = telegraphDuration;

        if (telegraphSound != null)
        {
            telegraphSound.Play();
        }

        GD.Print("Enemy: TELEGRAPH");
    }

    private void CalculateAttackDirection()
    {
        Vector2 direction = player.GlobalPosition - GlobalPosition;

        if (direction.LengthSquared() <= 0.001f)
        {
            attackDirection = Vector2.Right;
            return;
        }

        attackDirection = direction.Normalized();
    }

    // --------------------------------------------------
    // HIT PLAYER
    // --------------------------------------------------

    private void OnAttackHitboxAreaEntered(Area2D area)
    {
        if (state != EnemyState.Attack)
        {
            return;
        }

        if (!area.IsInGroup("player_hurtbox"))
        {
            return;
        }

        GD.Print("ENEMY HIT PLAYER");

        if (player is Player playerScript)
        {
            playerScript.TakeDamage(1);
        }
    }

    // --------------------------------------------------
    // TAKE DAMAGE
    // --------------------------------------------------

    public void TakeDamage(int damage)
    {
        if (state == EnemyState.Dead)
        {
            return;
        }

        if (damage <= 0)
        {
            return;
        }

        hp -= damage;

        GD.Print($"Enemy HP: {hp}/{maxHp}");

        OnDamaged();

        if (hp <= 0)
        {
            Die();
        }
    }

    private void OnDamaged()
    {
        // Sau này có thể thêm:
        // - hit flash
        // - hit sound
        // - hit VFX
        // - knockback
    }

    // --------------------------------------------------
    // DEATH
    // --------------------------------------------------

    private void Die()
    {
        if (state == EnemyState.Dead)
        {
            return;
        }

        state = EnemyState.Dead;

        Velocity = Vector2.Zero;

        attackHitbox.Monitoring = false;

        GD.Print("ENEMY DEAD");

        QueueFree();
    }

    private void UpdateDead()
    {
        Velocity = Vector2.Zero;
    }

    // --------------------------------------------------
    // DEBUG VISUAL
    // --------------------------------------------------

    public override void _Draw()
    {
        if (state != EnemyState.Telegraph)
        {
            return;
        }

        float progress = 1f - (stateTimer / telegraphDuration);

        DrawLine(
            Vector2.Zero,
            attackDirection * 80f,
            Colors.Red,
            4f
        );

        DrawCircle(
            attackDirection * 80f,
            6f,
            Colors.Red
        );
    }
}
