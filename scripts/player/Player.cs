using Godot;
using System.Collections.Generic;

public partial class Player : CharacterBody2D
{
    [ExportGroup("Movement")]
    [Export] public float MoveSpeed = 300f;
    [Export] public float JumpVelocity = -500f;
    [Export] public float Gravity = 1400f;
    [Export] public float CoyoteTime = 0.08f;
    [Export] public float JumpBufferTime = 0.08f;

    [ExportGroup("Dodge")]
    [Export] public float DodgeSpeed = 700f;
    [Export] public float DodgeDuration = 0.12f;
    [Export] public float DodgeCooldown = 0.35f;

    [ExportGroup("Attack")]
    [Export] public float AttackDuration = 0.20f;
    [Export] public float AttackActiveStart = 0.06f;
    [Export] public float AttackActiveDuration = 0.08f;
    [Export] public float AttackCooldown = 0.22f;
    [Export] public float AttackDistance = 38f;
    [Export] public float SlashRadius = 48f;

    [ExportGroup("Parry")]
    [Export] public float ParryWindow = 0.22f;

    [ExportGroup("Damage response")]
    [Export] public float HitstunDuration = 0.16f;
    [Export] public float HitInvulnerabilityDuration = 0.28f;
    [Export] public float HitKnockbackSpeed = 150f;

    [ExportGroup("Health")]
    [Export] public int MaxHp = 3;

    [ExportGroup("Debug")]
    [Export] public bool DebugLogs = false;

    public Vector2 AimDirection { get; private set; }
    public bool IsAttacking => isAttacking;
    public bool IsDodging => isDodging;
    public bool IsParrying => isParrying;
    public bool IsHitstunned => isHitstunned;
    public bool IsDead => isDead;
    public int CurrentHp => hp;
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
    private float hitstunTimer;
    private float hitInvulnerabilityTimer;
    private bool isHitstunned;
    private bool isDead;

    private float parryFlashTimer;
    private float hitFlashTimer;
    private float hitVfxTimer;
    private Vector2 hitVfxPosition;
    private float slashFlashTimer;

    private void DebugLog(string message)
    {
        if (DebugLogs)
            GD.Print(message);
    }

    public override void _Ready()
    {
        hp = MaxHp;
        attackHitbox = GetNode<Area2D>("AttackHitbox");
        attackHitbox.AreaEntered += OnAttackHitboxAreaEntered;
        attackHitbox.Monitoring = false;

        PlayerVfx vfx = GetNodeOrNull<PlayerVfx>("PlayerVfx");
        vfx?.Bind(this);
        UpdateAimDirection();
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        UpdateTimers(dt);
        UpdateAimDirection();

        if (isDead)
        {
            Velocity = Vector2.Zero;
            return;
        }

        if (isHitstunned)
        {
            HandleGravity(dt);
            Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0f, 500f * dt), Velocity.Y);
            MoveAndSlide();
            return;
        }

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
        UpdateDamageTimers(delta);
        UpdateFxTimers(delta);
    }

    private void UpdateMovementTimers(float delta)
    {
        if (IsOnFloor())
            coyoteTimer = CoyoteTime;
        else
            coyoteTimer -= delta;

        if (jumpBufferTimer > 0f)
            jumpBufferTimer -= delta;
    }

    private void UpdateAttackTimers(float delta)
    {
        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= delta;

        if (!isAttacking)
            return;

        attackTimer += delta;
        float activeEnd = AttackActiveStart + AttackActiveDuration;

        if (!attackHitboxActive && attackTimer >= AttackActiveStart && attackTimer < activeEnd)
            BeginAttackHitbox();

        if (attackHitboxActive && attackTimer >= activeEnd)
            EndAttackHitbox();

        if (attackTimer >= AttackDuration)
            EndAttack();
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
        if (!isParrying)
            return;

        parryTimer -= delta;
        if (parryTimer <= 0f)
        {
            parryTimer = 0f;
            isParrying = false;
        }
    }

    private void UpdateDamageTimers(float delta)
    {
        if (isHitstunned)
        {
            hitstunTimer -= delta;
            if (hitstunTimer <= 0f)
            {
                hitstunTimer = 0f;
                isHitstunned = false;
            }
        }

        if (hitInvulnerabilityTimer > 0f)
            hitInvulnerabilityTimer -= delta;
    }

    private void UpdateFxTimers(float delta)
    {
        if (parryFlashTimer > 0f) parryFlashTimer -= delta;
        if (hitFlashTimer > 0f) hitFlashTimer -= delta;
        if (hitVfxTimer > 0f) hitVfxTimer -= delta;
        if (slashFlashTimer > 0f) slashFlashTimer -= delta;
    }

    private void HandleHorizontalMovement()
    {
        float direction = Input.GetAxis("move_left", "move_right");
        Velocity = new Vector2(direction * MoveSpeed, Velocity.Y);
    }

    private void HandleGravity(float delta)
    {
        if (!IsOnFloor())
            Velocity = new Vector2(Velocity.X, Velocity.Y + Gravity * delta);
        else if (Velocity.Y > 0f)
            Velocity = new Vector2(Velocity.X, 0f);
    }

    private void HandleJumpInput()
    {
        if (Input.IsActionJustPressed("jump"))
            jumpBufferTimer = JumpBufferTime;
    }

    private void HandleJump()
    {
        if (jumpBufferTimer <= 0f || coyoteTimer <= 0f)
            return;

        Velocity = new Vector2(Velocity.X, JumpVelocity);
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
    }

    private void HandleDodgeInput()
    {
        if (!Input.IsActionJustPressed("dodge") || isDodging || isParrying || dodgeCooldownTimer > 0f)
            return;

        if (!CanCancelAttackForDefense())
            return;

        float direction = Input.GetAxis("move_left", "move_right");
        if (Mathf.IsZeroApprox(direction))
            direction = AimDirection.X >= 0f ? 1f : -1f;

        dodgeDirection = Mathf.Sign(direction);
        isDodging = true;
        dodgeTimer = DodgeDuration;
        dodgeCooldownTimer = DodgeCooldown;
        Velocity = new Vector2(dodgeDirection * DodgeSpeed, 0f);
        DebugLog("PLAYER DODGE");
    }

    private void HandleDodgeMovement()
    {
        Velocity = new Vector2(dodgeDirection * DodgeSpeed, 0f);
    }

    private void UpdateAimDirection()
    {
        Vector2 direction = GetGlobalMousePosition() - GlobalPosition;
        if (direction.LengthSquared() > 0.001f)
            AimDirection = direction.Normalized();
    }

    private void Attack()
    {
        if (isDead || isHitstunned || isDodging || isParrying || isAttacking || attackCooldownTimer > 0f)
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
        attackHitbox.Position = AimDirection * AttackDistance;
        attackHitbox.Rotation = AimDirection.Angle();
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
        if (attackHitbox != null)
            attackHitbox.Monitoring = false;
    }

    private void EndAttack()
    {
        EndAttackHitbox();
        isAttacking = false;
        attackTimer = 0f;
        QueueRedraw();
    }

    /// <summary>
    /// The active frames are committed. Once they finish, dodge/parry may cancel the short recovery.
    /// </summary>
    private bool CanCancelAttackForDefense()
    {
        if (!isAttacking)
            return true;

        float activeEnd = AttackActiveStart + AttackActiveDuration;
        if (attackTimer < activeEnd)
            return false;

        EndAttack();
        return true;
    }

    private void Parry()
    {
        if (isDead || isHitstunned || isDodging || isParrying)
            return;

        if (!CanCancelAttackForDefense())
            return;

        isParrying = true;
        parryTimer = ParryWindow;
        DebugLog("PLAYER PARRY");
    }

    public bool TakeDamage(int damage)
    {
        return TakeDamage(damage, 0f);
    }

    public bool TakeDamage(int damage, float knockbackDirection)
    {
        if (isDead || damage <= 0)
            return false;

        if (isDodging)
        {
            DebugLog("PLAYER DODGED DAMAGE");
            return false;
        }

        if (isParrying)
        {
            DebugLog("PLAYER PARRIED");
            parryFlashTimer = 0.12f;
            return false;
        }

        if (hitInvulnerabilityTimer > 0f)
        {
            DebugLog("PLAYER HIT IGNORED (INVULNERABLE)");
            return false;
        }

        hp = Mathf.Max(0, hp - damage);
        hitFlashTimer = 0.12f;
        hitVfxPosition = ToLocal(GlobalPosition);
        hitVfxTimer = 0.12f;

        EndAttack();
        isParrying = false;
        isHitstunned = hp > 0;
        hitstunTimer = isHitstunned ? HitstunDuration : 0f;
        hitInvulnerabilityTimer = HitInvulnerabilityDuration;

        float direction = Mathf.Sign(knockbackDirection);
        Velocity = new Vector2(direction * HitKnockbackSpeed, Velocity.Y);
        DebugLog($"Player HP: {hp}");

        if (hp <= 0)
        {
            isDead = true;
            isHitstunned = false;
            isDodging = false;
            isParrying = false;
            Velocity = Vector2.Zero;
            DebugLog("PLAYER DEAD");
        }

        QueueRedraw();
        return true;
    }

    private void OnAttackHitboxAreaEntered(Area2D area)
    {
        if (!isAttacking || area == null || !area.IsInGroup("enemy_hurtbox"))
            return;

        if (area.GetParent() is not Enemy enemy || attackHitTargets.Contains(enemy))
            return;

        attackHitTargets.Add(enemy);
        GD.Print("PLAYER HIT ENEMY");
        enemy.TakeDamage(1);
        ShowHitVfx(area.GlobalPosition);
    }

    private void ShowHitVfx(Vector2 globalPosition)
    {
        hitVfxPosition = ToLocal(globalPosition);
        hitVfxTimer = 0.12f;
        QueueRedraw();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouseEvent || !mouseEvent.Pressed)
            return;

        if (mouseEvent.ButtonIndex == MouseButton.Left)
            Attack();
        else if (mouseEvent.ButtonIndex == MouseButton.Right)
            Parry();
    }
}
