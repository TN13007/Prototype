using Godot;


public partial class EncounterController : Node
{
    private const string WorldPath =
        "res://data/worlds/prototype_arena.json";

    private const string RosterPath =
        "res://data/enemies/prototype_roster.json";

    private const string PhrasePath =
        "res://data/music/phrases/prototype_battle.json";

    private const string EncounterPath =
        "res://data/encounters/prototype_encounter.json";


    public override void _Ready()
    {
        CombatDirector director =
            GetNodeOrNull<CombatDirector>("../CombatDirector");

        ThreatDispatcher dispatcher =
            GetNodeOrNull<ThreatDispatcher>("../ThreatDispatcher");

        CombatClock clock =
            GetNodeOrNull<CombatClock>("../CombatClock");

        WorldController world =
            GetNodeOrNull<WorldController>("../World");

        EnemyManager manager =
            GetNodeOrNull<EnemyManager>("../EnemyManager");

        MusicDirector music =
            GetNodeOrNull<MusicDirector>("../MusicDirector");

        if(director == null || dispatcher == null || clock == null ||
           world == null || manager == null || music == null)
        {
            GD.PushError("EncounterController is missing a required runtime service.");
            return;
        }

        PrototypeWorldConfig worldConfig =
            PrototypeContentLoader.Load<PrototypeWorldConfig>(WorldPath);

        EnemyRosterConfig roster =
            PrototypeContentLoader.Load<EnemyRosterConfig>(RosterPath);

        MusicPhraseConfig phrase =
            PrototypeContentLoader.Load<MusicPhraseConfig>(PhrasePath);

        EncounterConfig encounter =
            PrototypeContentLoader.Load<EncounterConfig>(EncounterPath);

        if(worldConfig == null || roster == null || phrase == null ||
           encounter == null || !world.Initialize(worldConfig))
        {
            return;
        }

        manager.Initialize(world, roster);

        EncounterTimeline timeline =
            new EncounterLoader().Compose(encounter, phrase);

        director.Initialize(timeline, dispatcher, clock);
        music.Play(phrase, clock);
    }
}
