using Godot;


public enum ThreatState
{
    Created,
    Dispatching,
    Reserved,
    Executing,
    Completed,
    Failed,
    Interrupted,
    Cancelled
}


public readonly struct ThreatResolution
{
    public ThreatState State { get; }

    public string Reason { get; }


    public ThreatResolution(ThreatState state, string reason)
    {
        State = state;
        Reason = reason;
    }
}


public sealed class ThreatRequest
{
    public int Id { get; }

    public ThreatType Type { get; }

    public float ImpactTime { get; }

    public Node2D Target { get; }

    public ThreatState State { get; private set; } = ThreatState.Created;

    public Enemy AssignedEnemy { get; private set; }

    public ThreatResolution? Resolution { get; private set; }

    public event System.Action<ThreatRequest, ThreatResolution> Resolved;


    public ThreatRequest(
        int id,
        ThreatType type,
        float impactTime,
        Node2D target)
    {
        Id = id;
        Type = type;
        ImpactTime = impactTime;
        Target = target;
    }


    public bool BeginDispatch()
    {
        if(State != ThreatState.Created)
            return false;

        State = ThreatState.Dispatching;
        return true;
    }


    public bool Reserve(Enemy enemy)
    {
        if(State != ThreatState.Dispatching || enemy == null)
            return false;

        AssignedEnemy = enemy;
        State = ThreatState.Reserved;
        return true;
    }


    public bool BeginExecution(Enemy enemy)
    {
        if(State != ThreatState.Reserved || AssignedEnemy != enemy)
            return false;

        State = ThreatState.Executing;
        return true;
    }


    public bool Resolve(ThreatState finalState, string reason)
    {
        if(!IsTerminal(finalState) || IsTerminal(State))
            return false;

        State = finalState;
        Resolution = new ThreatResolution(finalState, reason);
        Resolved?.Invoke(this, Resolution.Value);
        return true;
    }


    public static bool IsTerminal(ThreatState state)
    {
        return state == ThreatState.Completed ||
               state == ThreatState.Failed ||
               state == ThreatState.Interrupted ||
               state == ThreatState.Cancelled;
    }
}
