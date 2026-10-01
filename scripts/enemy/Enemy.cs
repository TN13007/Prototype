using Godot;

public partial class Enemy : CharacterBody2D
{
    private int hp = 3;
    private Area2D attackHitbox;
    private const float AttackDuration = 0.2f;
    private Node2D player;
    private AudioStreamPlayer2D telegraphSound;
    private float attackSpeed = 500f;
    private enum EnemyState
    {
        Idle,
        Telegraph,
        Attack,
        Recovery
    }

    private EnemyState state = EnemyState.Idle;
    private float stateTimer;
    private Vector2 attackDirection;

    public override void _Ready()
    {

        player = GetTree().GetFirstNodeInGroup("player") as Node2D;

        attackHitbox = GetNode<Area2D>("AttackHitbox");
        attackHitbox.AreaEntered += OnAttackHitboxAreaEntered;
        attackHitbox.Monitoring = false;

        GD.Print($"Enemy found player: {player != null}");
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        telegraphSound = GetNode<AudioStreamPlayer2D>("TelegraphSound");

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
        }

        QueueRedraw();
    }

    private void UpdateIdle()
    {
    }

    private void UpdateTelegraph(float delta)
    {
        stateTimer -= delta;

        if (stateTimer <= 0f)
        {
            state = EnemyState.Attack;
            stateTimer = AttackDuration;

            attackHitbox.Position = attackDirection * 30f;
            attackHitbox.Monitoring = true;

            GD.Print("Enemy: ATTACK");
        }
    }

    private void UpdateAttack(float delta)
    {
        Velocity = attackDirection * attackSpeed;
        MoveAndSlide();

        stateTimer -= delta;

        if (stateTimer <= 0f)
        {
            Velocity = Vector2.Zero;

            state = EnemyState.Recovery;
            stateTimer = 0.5f;

            attackHitbox.Monitoring = false;
            attackHitbox.Position = Vector2.Zero;

            GD.Print("Enemy: RECOVERY");
        }
    }

    private void UpdateRecovery(float delta)
    {
        stateTimer -= delta;

        if (stateTimer <= 0f)
        {
            state = EnemyState.Idle;

            GD.Print("Enemy: IDLE");
        }
    }

    public void StartAttack()
    {
        if (state != EnemyState.Idle)
            return;

        attackDirection =
                (player.GlobalPosition - GlobalPosition).Normalized();

        state = EnemyState.Telegraph;
        stateTimer = 0.5f;

        telegraphSound.Play();

        GD.Print("Enemy: TELEGRAPH");
    }
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent &&
            keyEvent.Pressed &&
            keyEvent.Keycode == Key.Space)
        {
            StartAttack();
        }
    }
    public override void _Draw()
    {
        if (state != EnemyState.Telegraph)
            return;

        DrawLine(
            Vector2.Zero,
            attackDirection * 80f,
            Colors.Red,
            4f
        );
    }
    private void OnAttackHitboxAreaEntered(Area2D area)
    {
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
    public void TakeDamage(int damage)
    {
        hp -= damage;

        GD.Print($"Enemy HP: {hp}");

        if (hp <= 0)
        {
            Die();
        }
    }
    private void Die()
    {
        GD.Print("ENEMY DEAD");

        QueueFree();
    }
}
