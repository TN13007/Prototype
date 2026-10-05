using Godot;
using System.Collections.Generic;

public partial class Player : CharacterBody2D
{
    [ExportGroup("Movement")]
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

    [ExportGroup("Dodge")]
    [Export]
    public float DodgeSpeed = 700f;

    [Export]
    public float DodgeDuration = 0.12f;

    [Export]
    public float DodgeCooldown = 0.35f;

    [ExportGroup("Attack")]
    [Export]
    public float AttackDuration = 0.20f;

    [Export]
    public float AttackActiveStart = 0.06f;

    [Export]
    public float AttackActiveDuration = 0.08f;

    [Export]
    public float AttackCooldown = 0.22f;

    [Export]
    public float AttackDistance = 38f;

    [Export]
    public float SlashRadius = 48f;

    [ExportGroup("Parry")]
    [Export]
    public float ParryWindow = 0.22f;

    [ExportGroup("Health")]
    [Export]
    public int MaxHp = 3;
    [ExportGroup("Debug")]
    [Export]
    public bool DebugLogs = false;

    public Vector2 AimDirection { get; private set; }

    public bool IsAttacking => isAttacking;

    public bool IsDodging => isDodging;

    public bool IsParrying => isParrying;

    public float AttackTimer => attackTimer;

    public float SlashFlashTimer => slashFlashTimer;

    public float DodgeDirection => dodgeDirection;

    public float HitVfxTimer => hitVfxTimer;

    public Vector2 HitVfxPosition => hitVfxPosition;

    public float ParryFlashTimer => parryFlashTimer;

    public float HitFlashTimer => hitFlashTimer;

    private Area2D attackHitbox;

    private int hp;

    private float coyoteTimer;
    private float jumpBufferTimer;

    private float attackTimer;
    private float attackCooldownTimer;

    private bool isAttacking;
    private bool attackHitboxActive;

    private readonly HashSet<Enemy> attackHitTargets = new();

    private float dodgeTimer;
    private float dodgeCooldownTimer;

    private bool isDodging;
    private float dodgeDirection;

    private float parryTimer;
    private bool isParrying;

    private float parryFlashTimer;
    private float hitFlashTimer;

    private float hitVfxTimer;
    private Vector2 hitVfxPosition;

    private float slashFlashTimer;

    private void DebugLog(string message)
    {
        if (DebugLogs)
            DebugLog(message);
    }


    public override void _Ready()
    {
        hp = MaxHp;

        attackHitbox = GetNode<Area2D>("AttackHitbox");

        attackHitbox.AreaEntered += OnAttackHitboxAreaEntered;
        attackHitbox.Monitoring = false;

        PlayerVfx vfx =
            GetNodeOrNull<PlayerVfx>("PlayerVfx");

        if (vfx != null)
            vfx.Bind(this);

        UpdateAimDirection();
    }


    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        UpdateTimers(dt);
        UpdateAimDirection();

        HandleJumpInput();
        HandleDodgeInput();

        if (isDodging)
        {
            HandleDodgeMovement();
        }
        else
        {
            HandleHorizontalMovement();
            HandleGravity(dt);
            HandleJump();
        }

        MoveAndSlide();
    }


    private void UpdateTimers(float delta)
    {
        UpdateMovementTimers(delta);
        UpdateAttackTimers(delta);
        UpdateDodgeTimers(delta);
        UpdateParryTimers(delta);
        UpdateFxTimers(delta);
    }


    private void UpdateMovementTimers(float delta)
    {
        if (IsOnFloor())
        {
            coyoteTimer = CoyoteTime;
        }
        else
        {
            coyoteTimer -= delta;
        }

        if (jumpBufferTimer > 0f)
            jumpBufferTimer -= delta;
    }


    private void UpdateAttackTimers(float delta)
    {
        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= delta;

        if (isAttacking)
        {
            attackTimer += delta;

            float activeEnd =
                AttackActiveStart + AttackActiveDuration;

            if (!attackHitboxActive &&
                attackTimer >= AttackActiveStart &&
                attackTimer < activeEnd)
            {
                BeginAttackHitbox();
            }

            if (attackHitboxActive &&
                attackTimer >= activeEnd)
            {
                EndAttackHitbox();
            }

            if (attackTimer >= AttackDuration)
            {
                EndAttack();
            }
        }
    }


    private void UpdateDodgeTimers(float delta)
    {
        if (isDodging)
        {
            dodgeTimer -= delta;

            if (dodgeTimer <= 0f)
            {
                dodgeTimer = 0f;
                isDodging = false;
            }
        }

        if (dodgeCooldownTimer > 0f)
            dodgeCooldownTimer -= delta;
    }


    private void UpdateParryTimers(float delta)
    {
        if (isParrying)
        {
            parryTimer -= delta;

            if (parryTimer <= 0f)
            {
                parryTimer = 0f;
                isParrying = false;
            }
        }
    }


    private void UpdateFxTimers(float delta)
    {
        if (parryFlashTimer > 0f)
            parryFlashTimer -= delta;

        if (hitFlashTimer > 0f)
            hitFlashTimer -= delta;

        if (hitVfxTimer > 0f)
            hitVfxTimer -= delta;

        if (slashFlashTimer > 0f)
            slashFlashTimer -= delta;
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
        else if (Velocity.Y > 0f)
        {
            Velocity = new Vector2(
                Velocity.X,
                0f
            );
        }
    }


    private void HandleJumpInput()
    {
        if (Input.IsActionJustPressed("jump"))
        {
            jumpBufferTimer = JumpBufferTime;
        }
    }


    private void HandleJump()
    {
        if (jumpBufferTimer <= 0f)
            return;

        if (coyoteTimer <= 0f)
            return;

        Velocity = new Vector2(
            Velocity.X,
            JumpVelocity
        );

        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
    }


    private void HandleDodgeInput()
    {
        if (!Input.IsActionJustPressed("dodge"))
            return;

        if (isDodging)
            return;

        if (dodgeCooldownTimer > 0f)
            return;

        float direction = Input.GetAxis(
            "move_left",
            "move_right"
        );

        if (Mathf.IsZeroApprox(direction))
        {
            direction =
                AimDirection.X >= 0f
                    ? 1f
                    : -1f;
        }

        dodgeDirection = Mathf.Sign(direction);

        isDodging = true;
        dodgeTimer = DodgeDuration;
        dodgeCooldownTimer = DodgeCooldown;

        Velocity = new Vector2(
            dodgeDirection * DodgeSpeed,
            0f
        );

        DebugLog("PLAYER DODGE");
    }


    private void HandleDodgeMovement()
    {
        Velocity = new Vector2(
            dodgeDirection * DodgeSpeed,
            0f
        );
    }


    private void UpdateAimDirection()
    {
        Vector2 mousePosition =
            GetGlobalMousePosition();

        Vector2 direction =
            mousePosition - GlobalPosition;

        if (direction.LengthSquared() > 0.001f)
        {
            AimDirection =
                direction.Normalized();
        }
    }


    private void Attack()
    {
        if (isDodging)
            return;

        if (isAttacking)
            return;

        if (attackCooldownTimer > 0f)
            return;

        isAttacking = true;
        attackTimer = 0f;
        attackCooldownTimer = AttackCooldown;

        attackHitTargets.Clear();

        slashFlashTimer = AttackDuration;

        UpdateAttackHitboxTransform();

        DebugLog("PLAYER ATTACK");

        QueueRedraw();
    }


    private void UpdateAttackHitboxTransform()
    {
        attackHitbox.Position =
            AimDirection * AttackDistance;

        attackHitbox.Rotation =
            AimDirection.Angle();
    }


    private void BeginAttackHitbox()
    {
        attackHitboxActive = true;

        UpdateAttackHitboxTransform();

        attackHitbox.Monitoring = true;

        DebugLog("PLAYER SLASH ACTIVE");
    }


    private void EndAttackHitbox()
    {
        attackHitboxActive = false;
        attackHitbox.Monitoring = false;
    }


    private void EndAttack()
    {
        EndAttackHitbox();

        isAttacking = false;
        attackTimer = 0f;

        QueueRedraw();
    }


    private void Parry()
    {
        if (isDodging)
            return;

        if (isParrying)
            return;

        isParrying = true;
        parryTimer = ParryWindow;

        DebugLog("PLAYER PARRY");
    }


    public bool TakeDamage(int damage)
    {
        if (isDodging)
        {
            DebugLog("PLAYER DODGED DAMAGE");
            return false;
        }

        if (isParrying)
        {
            DebugLog("PLAYER PARRIED");

            parryTimer = ParryWindow;
            isParrying = true;

            parryFlashTimer = 0.12f;

            return false;
        }

        hp -= damage;
        hp = Mathf.Max(hp, 0);

        hitFlashTimer = 0.12f;

        DebugLog($"Player HP: {hp}");

        if (hp <= 0)
        {
            DebugLog("PLAYER DEAD");
        }

        return true;
    }

    private void OnAttackHitboxAreaEntered(Area2D area)
    {
        DebugLog($"PLAYER ATTACK HIT: {area.Name}");

        if (!area.IsInGroup("enemy_hurtbox"))
        {
            DebugLog("Not enemy hurtbox");
            return;
        }

        if (area.GetParent() is not Enemy enemy)
        {
            DebugLog("Hurtbox parent is not Enemy");
            return;
        }

        if (attackHitTargets.Contains(enemy))
            return;

        attackHitTargets.Add(enemy);

        GD.Print("PLAYER HIT ENEMY");

        enemy.TakeDamage(1);

        ShowHitVfx(
            area.GlobalPosition
        );
    }


    private void ShowHitVfx(Vector2 globalPosition)
    {
        hitVfxPosition =
            ToLocal(globalPosition);

        hitVfxTimer = 0.12f;

        QueueRedraw();
    }


    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouseEvent)
            return;

        if (!mouseEvent.Pressed)
            return;

        if (mouseEvent.ButtonIndex == MouseButton.Left)
        {
            Attack();
        }
        else if (mouseEvent.ButtonIndex == MouseButton.Right)
        {
            Parry();
        }
    }


}
