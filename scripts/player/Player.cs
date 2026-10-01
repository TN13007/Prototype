using System.Threading.Tasks.Dataflow;
using Godot;

public partial class Player : CharacterBody2D
{
    [Export]
    public float MoveSpeed = 300f;

    [Export]
    public float JumpVelocity = -500f;

    [Export]
    public float Gravity = 1400f;

    [Export]
    public float CoyoteTime = 0.08f;

    [Export]
    public float JumpBufferTime = 0.08f;

    public Vector2 AimDirection { get; private set; }
    private Area2D attackHitbox;
    private int hp = 3;
    private float coyoteTimer = 0f;
    private float jumpBufferTimer = 0f;

    public override void _Ready()
    {
        attackHitbox = GetNode<Area2D>("AttackHitbox");
        attackHitbox.AreaEntered += OnAttackHitboxAreaEntered;
        attackHitbox.Monitoring = false;
    }

    private async void Attack()
    {
        GD.Print("PLAYER ATTACK");

        attackHitbox.Monitoring = true;

        await ToSignal(
            GetTree().CreateTimer(0.1f),
            SceneTreeTimer.SignalName.Timeout
        );

        attackHitbox.Monitoring = false;
    }
    private void OnAttackHitboxAreaEntered(Area2D area)
    {
        GD.Print($"PLAYER ATTACK HIT: {area.Name}");

        if (!area.IsInGroup("enemy_hurtbox"))
        {
            GD.Print("Not enemy hurtbox");
            return;
        }

        GD.Print("PLAYER HIT ENEMY");

        if (area.GetParent() is Enemy enemy)
        {
            enemy.TakeDamage(1);
        }
    }
    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        HandleTimers(dt);
        HandleHorizontalMovement();
        HandleGravity(dt);
        HandleJump();

        MoveAndSlide();

        UpdateAimDirection();

        QueueRedraw();
    }


    private void HandleTimers(float delta)
    {
        if (IsOnFloor())
        {
            coyoteTimer = CoyoteTime;
        }
        else
        {
            coyoteTimer -= delta;
        }


        if (Input.IsActionJustPressed("jump"))
        {
            jumpBufferTimer = JumpBufferTime;
        }
        else
        {
            jumpBufferTimer -= delta;
        }
    }

    public void TakeDamage(int damage)
    {
        hp -= damage;

        GD.Print($"Player HP: {hp}");
    }
    private void HandleHorizontalMovement()
    {
        float direction = Input.GetAxis(
            "move_left",
            "move_right"
        );

        Velocity = new Vector2(
            direction * MoveSpeed,
            Velocity.Y
        );
    }


    private void HandleGravity(float delta)
    {
        if (!IsOnFloor())
        {
            Velocity = new Vector2(
                Velocity.X,
                Velocity.Y + Gravity * delta
            );
        }
    }


    private void HandleJump()
    {
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            Velocity = new Vector2(
                Velocity.X,
                JumpVelocity
            );

            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }
    }


    private void UpdateAimDirection()
    {
        Vector2 mousePosition = GetGlobalMousePosition();

        AimDirection = (
            mousePosition - GlobalPosition
        ).Normalized();
    }


    public override void _Draw()
    {
        DrawLine(
            Vector2.Zero,
            AimDirection * 50f,
            Colors.Yellow,
            3f
        );
    }
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent &&
            keyEvent.Pressed &&
            keyEvent.Keycode == Key.J)
        {
            Attack();
        }
    }
}
