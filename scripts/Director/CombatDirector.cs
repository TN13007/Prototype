using Godot;
using System.Collections.Generic;


public partial class CombatDirector : Node
{
    private EncounterTimeline timeline;

    private CombatClock clock;

    private ThreatDispatcher dispatcher;

    private Node2D player;

    private EncounterState state =
        EncounterState.Finished;

    private int nextThreatId = 1;

    private int plannedThreatCount;

    private int resolvedThreatCount;

    private bool warnedMissingDependencies;


    public void Initialize(
        EncounterTimeline encounter,
        ThreatDispatcher threatDispatcher,
        CombatClock combatClock)
    {
        timeline = encounter;
        dispatcher = threatDispatcher;
        clock = combatClock;

        ResolvePlayer();

        if(timeline == null ||
           dispatcher == null ||
           clock == null ||
           player == null)
        {
            GD.PushError(
                "DIRECTOR: Cannot initialize encounter. " +
                $"timeline: {timeline != null}, " +
                $"dispatcher: {dispatcher != null}, " +
                $"clock: {clock != null}, " +
                $"player: {player != null}");

            state =
                EncounterState.Finished;

            return;
        }

        PlanEncounter();

        state =
            EncounterState.Running;

        GD.Print(
            $"DIRECTOR: ENCOUNTER READY " +
            $"({plannedThreatCount} threats)");
    }


    public override void _Ready()
    {
        GetTree()
            .NodeAdded +=
            OnTreeNodeAdded;

        ResolvePlayer();
    }


    public override void _ExitTree()
    {
        if(GetTree() != null)
        {
            GetTree().NodeAdded -=
                OnTreeNodeAdded;
        }
    }


    private void ResolvePlayer()
    {
        if(player != null &&
           IsInstanceValid(player))
            return;

        Node found =
            GetTree()
            .GetFirstNodeInGroup("player");

        if(found == null)
        {
            GD.PushError(
                "DIRECTOR: No node found in group " +
                "'player'");

            return;
        }

        if(found is not Node2D node2D)
        {
            GD.PushError(
                "DIRECTOR: Group 'player' must be " +
                "a Node2D");

            return;
        }

        player = node2D;
    }


    private void OnTreeNodeAdded(
        Node node)
    {
        if(player != null &&
           IsInstanceValid(player))
            return;

        if(node is not Node2D node2D)
            return;

        if(!node2D.IsInGroup("player"))
            return;

        player = node2D;
    }


    private void PlanEncounter()
    {
        plannedThreatCount = 0;
        resolvedThreatCount = 0;

        foreach(BeatEvent beat in timeline.Events)
        {
            CreateThreat(beat);
        }
    }


    private void CreateThreat(
        BeatEvent beat)
    {
        ThreatRequest request =
            new ThreatRequest(
                nextThreatId++,
                beat.ThreatType,
                beat.Time,
                player
            );

        request.Resolved +=
            OnThreatResolved;

        plannedThreatCount++;

        GD.Print(
            $"DIRECTOR: PLAN THREAT #{request.Id} " +
            $"{beat.ThreatType} " +
            $"impact @ {request.ImpactTime}"
        );

        dispatcher.Dispatch(
            request);
    }


    public override void _Process(
        double delta)
    {
        if(state ==
           EncounterState.Finished)
            return;

        if(timeline == null ||
           dispatcher == null ||
           clock == null ||
           player == null)
        {
            if(!warnedMissingDependencies)
            {
                warnedMissingDependencies = true;

                GD.PushWarning(
                    "DIRECTOR: Missing dependencies " +
                    $"(timeline: {timeline != null}, " +
                    $"dispatcher: {dispatcher != null}, " +
                    $"clock: {clock != null}, " +
                    $"player: {player != null})");
            }

            return;
        }

        if(plannedThreatCount == 0)
        {
            Finish();
            return;
        }

        if(resolvedThreatCount >=
           plannedThreatCount)
        {
            Finish();
        }
    }


    private void OnThreatResolved(
        ThreatRequest request,
        ThreatResolution resolution)
    {
        resolvedThreatCount++;

        GD.Print(
            $"DIRECTOR: THREAT #{request.Id} " +
            $"{resolution.State} " +
            $"({resolution.Reason}) " +
            $"[{resolvedThreatCount}/{plannedThreatCount}]"
        );
    }


    private void Finish()
    {
        if(state ==
           EncounterState.Finished)
            return;

        state =
            EncounterState.Finished;

        GD.Print(
            "DIRECTOR: ENCOUNTER END");
    }
}
