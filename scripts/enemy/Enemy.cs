using Godot;
using System;
using System.Collections.Generic;

public abstract partial class Enemy : CharacterBody2D
{
    [Export] public int Health { get; set; } = 1;
    [Export] public EnemyType EnemyTypeId { get; set; } = EnemyType.Chaser;
    [Export] public float StunDuration { get; set; } = 0.35f;
    [Export] public float ParryStunDuration { get; set; } = 0.70f;
    [Export] public float AttackHitboxOffset { get; set; } = 25f;

    protected EnemyState state = EnemyState.Approaching;
    protected Player player;
    protected CombatCoordinator coordinator;
    protected Area2D attackHitbox;

    private readonly HashSet<Player> hitPlayersThisStrike = new();
    private float stunTimer;

    public event Action<Enemy> Defeated;

    public EnemyState State => state;
    public bool IsAlive => state != EnemyState.Dead;

    public virtual bool CanReceiveAttackTurn => state == EnemyState.WaitingForTurn;

    protected bool ContactedPlayerThisStrike { get; private set; }

    public override void _Ready()
    {
        AddToGroup("enemy");

        player = GetTree().GetFirstNodeInGroup("player") as Player;
        coordinator = GetTree().GetFirstNodeInGroup("combat_coordinator") as CombatCoordinator;
        attackHitbox = GetNodeOrNull<Area2D>("AttackHitbox");

        if (attackHitbox != null)
        {
            attackHitbox.AreaEntered += OnAttackHitboxAreaEntered;
            attackHitbox.Monitoring = false;
        }

        EnemyManager manager = GetTree().GetFirstNodeInGroup("enemy_manager") as EnemyManager;
        manager?.Register(this);
    }

    public override void _ExitTree()
    {
        coordinator?.CancelEnemy(this);
        EnemyManager manager = GetTree().GetFirstNodeInGroup("enemy_manager") as EnemyManager;
        manager?.Unregister(this);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (state == EnemyState.Dead)
            return;

        float dt = (float)delta;
        if (state == EnemyState.Stunned)
        {
            stunTimer -= dt;
            if (stunTimer <= 0f)
            {
                stunTimer = 0f;
                ChangeState(EnemyState.Approaching);
                OnStunEnded();
            }
        }

        UpdateEnemy(dt);
        MoveAndSlide();
    }

    protected abstract void UpdateEnemy(float delta);

    public bool BeginAttackTurn(float firstImpactTime)
    {
        if (!CanReceiveAttackTurn || player == null || player.IsDead)
            return false;

        OnAttackTurnGranted(firstImpactTime);
        return true;
    }

    protected abstract void OnAttackTurnGranted(float firstImpactTime);

    public void RequestAttackTurn()
    {
        if (state != EnemyState.Approaching && state != EnemyState.WaitingForTurn)
            return;

        if (state != EnemyState.WaitingForTurn)
            ChangeState(EnemyState.WaitingForTurn);

        coordinator?.RequestAttack(this);
    }

    public void CancelAttackRequest()
    {
        coordinator?.CancelRequest(this);
        if (state == EnemyState.WaitingForTurn)
            ChangeState(EnemyState.Approaching);
    }

    public void ReleaseAttackTurn()
    {
        coordinator?.ReleaseTurn(this);
    }

    protected void ChangeState(EnemyState newState)
    {
        if (state == newState)
            return;

        state = newState;
        GD.Print($"{Name}: {state}");
        QueueRedraw();
    }

    protected void BeginAttackHitbox(float horizontalDirection)
    {
        ContactedPlayerThisStrike = false;
        hitPlayersThisStrike.Clear();

        if (attackHitbox == null)
            return;

        float direction = Mathf.Sign(horizontalDirection);
        if (Mathf.IsZeroApprox(direction))
            direction = 1f;

        attackHitbox.Position = new Vector2(direction * AttackHitboxOffset, 0f);
        // Area2D monitoring cannot be toggled synchronously while physics signals are running.
        attackHitbox.SetDeferred("monitoring", true);
    }

    protected void EndAttackHitbox()
    {
        if (attackHitbox != null)
        {
            // This can be called from AreaEntered (for example, when the player parries).
            // Defer the property change because the physics space is locked during the signal.
            attackHitbox.SetDeferred("monitoring", false);
        }
    }

    public virtual void TakeDamage(int damage)
    {
        if (state == EnemyState.Dead || damage <= 0)
            return;

        Health -= damage;
        GD.Print($"{Name}: took {damage} damage; HP={Mathf.Max(Health, 0)}");

        if (Health <= 0)
        {
            Kill();
            return;
        }

        // A hit interrupts an active action, but repeated hits do not refresh stun forever.
        if (state != EnemyState.Stunned)
            ApplyStun(StunDuration, "Hit by player");
    }

    public void OnParried()
    {
        if (state == EnemyState.Dead)
            return;

        GD.Print($"{Name}: ATTACK PARRIED");
        ApplyStun(ParryStunDuration, "Attack parried");
    }

    public virtual void Kill()
    {
        if (state == EnemyState.Dead)
            return;

        EndAttackHitbox();
        coordinator?.CancelEnemy(this);
        ChangeState(EnemyState.Dead);
        OnDeathStarted();
        Defeated?.Invoke(this);
        QueueFree();
    }

    protected virtual void OnStunStarted() { }
    protected virtual void OnStunEnded() { }
    protected virtual void OnDeathStarted() { }

    private void ApplyStun(float duration, string reason)
    {
        if (state == EnemyState.Dead || state == EnemyState.Stunned)
            return;

        EndAttackHitbox();
        coordinator?.CancelEnemy(this);
        stunTimer = Mathf.Max(0.05f, duration);
        ChangeState(EnemyState.Stunned);
        GD.Print($"{Name}: STUNNED ({reason})");
        OnStunStarted();
    }

    private void OnAttackHitboxAreaEntered(Area2D area)
    {
        if (state != EnemyState.Attacking || area == null || !area.IsInGroup("player_hurtbox"))
            return;

        if (area.GetParent() is not Player target || hitPlayersThisStrike.Contains(target))
            return;

        hitPlayersThisStrike.Add(target);
        ContactedPlayerThisStrike = true;

        float hitDirection = Mathf.Sign(target.GlobalPosition.X - GlobalPosition.X);
        if (Mathf.IsZeroApprox(hitDirection))
            hitDirection = 1f;

        bool tookDamage = target.TakeDamage(1, hitDirection);
        if (tookDamage)
        {
            GD.Print($"{Name}: PLAYER HIT");
        }
        else if (target.IsParrying)
        {
            OnParried();
        }
        else if (target.IsDodging)
        {
            GD.Print($"{Name}: PLAYER DODGED ATTACK");
        }
        else
        {
            GD.Print($"{Name}: hit ignored (player protected)");
        }
    }
}
