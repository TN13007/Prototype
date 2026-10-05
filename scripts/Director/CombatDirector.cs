using Godot;


public partial class CombatDirector : Node
{
    private EncounterTimeline timeline;


    private int currentIndex;


    private CombatClock clock;

    private int nextThreatId = 1;


    private EncounterState state =
        EncounterState.Finished;


    private ThreatDispatcher dispatcher;


    private Node2D player;

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


        state =
            EncounterState.Running;


        GD.Print(
            "DIRECTOR: ENCOUNTER START");
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

        if (found == null)
        {
            GD.PushError(
                "DIRECTOR: No node found in group " +
                "'player'");

            return;
        }

        if (found is not Node2D node2D)
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


    public override void _Process(
        double delta)
    {
        if(state ==
            EncounterState.Finished)
            return;


        ResolvePlayer();

        if(timeline == null ||
           dispatcher == null ||
           player == null)
        {
            if(!warnedMissingDependencies)
            {
                warnedMissingDependencies = true;

                GD.PushWarning(
                    "DIRECTOR: Missing dependencies " +
                    $"(timeline: {timeline != null}, " +
                    $"dispatcher: {dispatcher != null}, " +
                    $"player: {player != null})");
            }

            return;
        }

        if(clock == null || !clock.IsRunning)
            return;


        while(
            currentIndex <
            timeline.Events.Count)
        {
            BeatEvent beat =
                timeline.Events[currentIndex];


            if(clock.Time < beat.Time)
                break;


            CreateThreat(
                beat);


            currentIndex++;
        }


        if(currentIndex >=
           timeline.Events.Count)
        {
            Finish();
        }
    }



    private void CreateThreat(
        BeatEvent beat)
    {
        ThreatRequest request =
            new ThreatRequest(
                nextThreatId++,
                beat.ThreatType,
                beat.Time + beat.LeadTime,
                player
            );

        request.Resolved += OnThreatResolved;


        GD.Print(
            $"DIRECTOR: THREAT #{request.Id} {beat.ThreatType} " +
            $"impact @ {request.ImpactTime}"
        );


        dispatcher.Dispatch(
            request);
    }


    private void OnThreatResolved(
        ThreatRequest request,
        ThreatResolution resolution)
    {
        GD.Print(
            $"DIRECTOR: THREAT #{request.Id} " +
            $"{resolution.State} ({resolution.Reason})");
    }



    private void Finish()
    {
        state =
            EncounterState.Finished;


        GD.Print(
            "DIRECTOR: ENCOUNTER END");
    }
}
