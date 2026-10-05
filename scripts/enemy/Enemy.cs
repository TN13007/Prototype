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

    private readonly List<ThreatRequest> scheduledThreats =
        new();

    protected Node2D player;

    protected List<EnemyCapability> capabilities =
        new();

    public EnemyState State =>
        state;

    public int ScheduledThreatCount =>
        scheduledThreats.Count +
        (currentThreat != null ? 1 : 0);

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

        ActivateNextScheduledThreat();

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
        return state != EnemyState.Dead;
    }

    public virtual float GetApproachDistance(
        ThreatRequest request,
        EnemyCapability capability)
    {
        if(request == null ||
           capability == null ||
           request.Target == null)
        {
            return 0f;
        }

        float distance =
            GlobalPosition.DistanceTo(
                request.Target.GlobalPosition);

        return Mathf.Max(
            0f,
            distance - capability.AttackRange);
    }

    public bool TryGetCandidateScore(
        ThreatRequest request,
        CombatClock clock,
        out float score)
    {
        score = float.MinValue;

        if(request == null ||
           clock == null ||
           state == EnemyState.Dead)
        {
            return false;
        }

        EnemyCapability capability =
            GetCapability(request.Type);

        if(capability == null ||
           capability.ApproachSpeed <= 0f)
        {
            return false;
        }

        if(request.ImpactTime <= clock.Time)
            return false;

        float approachDistance =
            GetApproachDistance(
                request,
                capability);

        float requiredTime =
            approachDistance /
            capability.ApproachSpeed +
            capability.PreparationTime;

        float remainingTime =
            request.ImpactTime -
            clock.Time;

        if(requiredTime > remainingTime)
            return false;

        if(!CanScheduleThreat(
            request,
            capability))
        {
            return false;
        }

        float timingSlack =
            remainingTime -
            requiredTime;

        int load =
            ScheduledThreatCount;

        score =
            1000f -
            load * 200f -
            approachDistance * 0.1f -
            timingSlack * 0.01f;

        return true;
    }

    public bool TryReserveThreat(
        ThreatRequest request)
    {
        if(request == null ||
           state == EnemyState.Dead ||
           !CanHandle(request.Type))
        {
            return false;
        }

        if(currentThreat == request ||
           scheduledThreats.Contains(request))
        {
            return false;
        }

        EnemyCapability capability =
            GetCapability(request.Type);

        if(capability == null ||
           !CanScheduleThreat(
               request,
               capability))
        {
            return false;
        }

        scheduledThreats.Add(request);

        scheduledThreats.Sort(
            (a, b) =>
                a.ImpactTime.CompareTo(
                    b.ImpactTime));

        return true;
    }

    public void ReleaseReservation(
        ThreatRequest request)
    {
        if(request == null)
            return;

        scheduledThreats.Remove(request);
    }

    private bool CanScheduleThreat(
        ThreatRequest request,
        EnemyCapability capability)
    {
        if(request == null ||
           capability == null)
        {
            return false;
        }

        float minimumGap =
            Mathf.Max(
                capability.PreparationTime,
                0.1f);

        float previousImpact =
            float.NegativeInfinity;

        float nextImpact =
            float.PositiveInfinity;

        if(currentThreat != null)
        {
            if(currentThreat.ImpactTime <=
               request.ImpactTime)
            {
                previousImpact =
                    currentThreat.ImpactTime;
            }
            else
            {
                nextImpact =
                    currentThreat.ImpactTime;
            }
        }

        foreach(
            ThreatRequest scheduled
            in scheduledThreats)
        {
            if(scheduled == request)
                continue;

            if(scheduled.ImpactTime <=
               request.ImpactTime &&
               scheduled.ImpactTime >
               previousImpact)
            {
                previousImpact =
                    scheduled.ImpactTime;
            }

            if(scheduled.ImpactTime >
               request.ImpactTime &&
               scheduled.ImpactTime <
               nextImpact)
            {
                nextImpact =
                    scheduled.ImpactTime;
            }
        }

        if(previousImpact !=
           float.NegativeInfinity &&
           request.ImpactTime - previousImpact <
           minimumGap)
        {
            return false;
        }

        if(nextImpact !=
           float.PositiveInfinity &&
           nextImpact - request.ImpactTime <
           minimumGap)
        {
            return false;
        }

        return true;
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
        if(request == null ||
           !scheduledThreats.Contains(request) ||
           !CanHandle(request.Type))
        {
            return false;
        }

        GD.Print(
            $"{Name}: Threat scheduled #{request.Id}");

        return true;
    }

    private void ActivateNextScheduledThreat()
    {
        if(state != EnemyState.Idle ||
           currentThreat != null)
        {
            return;
        }

        while(scheduledThreats.Count > 0)
        {
            ThreatRequest nextThreat =
                scheduledThreats[0];

            scheduledThreats.RemoveAt(0);

            if(!nextThreat.BeginExecution(this))
            {
                nextThreat.Resolve(
                    ThreatState.Failed,
                    "Could not begin execution");

                continue;
            }

            currentThreat =
                nextThreat;

            ChangeState(
                EnemyState.Planning);

            return;
        }
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

        foreach(
            ThreatRequest scheduled
            in scheduledThreats)
        {
            scheduled.Resolve(
                ThreatState.Interrupted,
                "Actor was destroyed");
        }

        scheduledThreats.Clear();

        state =
            EnemyState.Dead;

        QueueFree();
    }

    public virtual void TakeDamage(int damage)
    {
        if(state == EnemyState.Dead ||
           damage <= 0)
        {
            return;
        }

        Health -= damage;

        if(Health <= 0)
            Kill();
    }

    protected void ResolveCurrentThreat(
        ThreatState finalState,
        string reason)
    {
        ThreatRequest threat =
            currentThreat;

        currentThreat = null;

        threat?.Resolve(
            finalState,
            reason);
    }
}
