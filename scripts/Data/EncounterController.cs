using Godot;

public partial class EncounterController : Node
{
    private const string WorldPath = "res://data/worlds/prototype_arena.json";
    private const string RosterPath = "res://data/enemies/prototype_roster.json";
    private const string PhrasePath = "res://data/music/phrases/prototype_battle.json";
    private const string EncounterPath = "res://data/encounters/prototype_encounter.json";

    public override void _Ready()
    {
        // Initial enemies also register in _Ready. Defer setup until every sibling has finished _Ready.
        CallDeferred(nameof(InitializeEncounter));
    }

    private void InitializeEncounter()
    {
        CombatDirector director = GetNodeOrNull<CombatDirector>("../CombatDirector");
        CombatClock clock = GetNodeOrNull<CombatClock>("../CombatClock");
        WorldController world = GetNodeOrNull<WorldController>("../World");
        EnemyManager manager = GetNodeOrNull<EnemyManager>("../EnemyManager");
        CombatCoordinator coordinator = GetNodeOrNull<CombatCoordinator>("../CombatCoordinator");
        MusicDirector music = GetNodeOrNull<MusicDirector>("../MusicDirector");
        Player player = GetTree().GetFirstNodeInGroup("player") as Player;

        if (director == null || clock == null || world == null || manager == null ||
            coordinator == null || music == null || player == null)
        {
            GD.PushError("ENCOUNTER: One or more runtime services are missing from Main scene");
            return;
        }

        PrototypeWorldConfig worldConfig = PrototypeContentLoader.Load<PrototypeWorldConfig>(WorldPath);
        EnemyRosterConfig roster = PrototypeContentLoader.Load<EnemyRosterConfig>(RosterPath);
        MusicPhraseConfig phrase = PrototypeContentLoader.Load<MusicPhraseConfig>(PhrasePath);
        EncounterConfig encounter = PrototypeContentLoader.Load<EncounterConfig>(EncounterPath);

        if (worldConfig == null || roster == null || phrase == null || encounter == null)
        {
            GD.PushError("ENCOUNTER: Could not load the world, roster, music phrase or encounter config");
            return;
        }

        if (!world.Initialize(worldConfig))
            return;

        manager.Initialize(world, roster);
        coordinator.Initialize(manager, clock, phrase.Bpm);

        // CombatClock follows audio playback when available; fallback clock still supports beat-grid timing.
        if (!music.Play(phrase, clock))
        {
            GD.PushWarning("ENCOUNTER: Music failed to start; using the internal combat clock");
            clock.Start();
        }

        director.Initialize(manager, coordinator, clock, player);
        GD.Print(
            $"ENCOUNTER: {encounter.Id} loaded. Enemy attacks are autonomous; " +
            "encounter sections no longer pre-plan individual strikes.");
    }
}
