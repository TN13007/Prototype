using Godot;
using System.Collections.Generic;


public abstract partial class Enemy : CharacterBody2D
{
    [Export]
    public int Health { get; set; } = 1;

    [Export]
    public EnemyType EnemyTypeId { get; set; } = EnemyType.Chaser;

    protected EnemyState state =
        EnemyState.Idle;



    protected ThreatRequest currentThreat;

    private ThreatRequest reservedThreat;



    protected Node2D player;



    protected List<EnemyCapability> capabilities =
        new();



    public EnemyState State =>
        state;



    public override void _Ready()
    {
        player =
            GetTree()
            .GetFirstNodeInGroup("player")
            as Node2D;


        RegisterCapabilities();


        EnemyManager manager =
            GetTree()
            .GetFirstNodeInGroup("enemy_manager")
            as EnemyManager;


        manager?.Register(this);
    }



    public override void _PhysicsProcess(
        double delta)
    {
        if(state ==
            EnemyState.Dead)
            return;


        UpdateEnemy(
            (float)delta);
    }



    protected abstract void UpdateEnemy(
        float delta);



    protected abstract void RegisterCapabilities();



    // =========================
    // CAPABILITY
    // =========================


    public bool CanHandle(
        ThreatType type)
    {
        foreach(
            EnemyCapability capability
            in capabilities)
        {
            if(capability.Type == type)
                return true;
        }


        return false;
    }



    public virtual bool IsAvailable()
    {
        return
            state == EnemyState.Idle &&
            reservedThreat == null;
    }


    public bool TryGetCandidateScore(
        ThreatRequest request,
        CombatClock clock,
        out float score)
    {
        score = float.MinValue;

        if(request == null || clock == null || !IsAvailable())
            return false;

        EnemyCapability capability = GetCapability(request.Type);

        if(capability == null || capability.ApproachSpeed <= 0f)
            return false;

        float distance = request.Target == null
            ? 0f
            : GlobalPosition.DistanceTo(request.Target.GlobalPosition);

        float requiredTime =
            distance / capability.ApproachSpeed +
            capability.PreparationTime;

        float remainingTime = request.ImpactTime - clock.Time;

        if(requiredTime > remainingTime)
            return false;

        float timingSlack = remainingTime - requiredTime;

        score =
            1000f -
            distance * 0.1f -
            timingSlack * 0.01f;

        return true;
    }


    public bool TryReserveThreat(ThreatRequest request)
    {
        if(request == null || !IsAvailable())
            return false;

        reservedThreat = request;
        return true;
    }


    public void ReleaseReservation(ThreatRequest request)
    {
        if(reservedThreat == request && currentThreat == null)
            reservedThreat = null;
    }



    public EnemyCapability GetCapability(
        ThreatType type)
    {
        foreach(
            EnemyCapability capability
            in capabilities)
        {
            if(capability.Type == type)
                return capability;
        }


        return null;
    }



    // =========================
    // THREAT
    // =========================


    public virtual bool ReceiveThreat(
        ThreatRequest request)
    {
        if(request == null || reservedThreat != request ||
           !CanHandle(request.Type) ||
           !request.BeginExecution(this))
            return false;


        currentThreat =
            request;


        ChangeState(
            EnemyState.Planning);


        GD.Print(
            $"{Name}: Threat received {request.Type}");

        return true;
    }



    protected void ChangeState(
        EnemyState newState)
    {
        state = newState;


        GD.Print(
            $"{Name}: {state}");
    }



    public virtual void Kill()
    {
        ResolveCurrentThreat(
            ThreatState.Interrupted,
            "Actor was destroyed");

        state =
            EnemyState.Dead;


        QueueFree();
    }


    public virtual void TakeDamage(int damage)
    {
        if(state == EnemyState.Dead || damage <= 0)
            return;

        Health -= damage;

        if(Health <= 0)
            Kill();
    }


    protected void ResolveCurrentThreat(
        ThreatState finalState,
        string reason)
    {
        ThreatRequest threat = currentThreat ?? reservedThreat;

        currentThreat = null;
        reservedThreat = null;

        threat?.Resolve(finalState, reason);
    }
}
