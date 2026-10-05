using System.Collections.Generic;


public sealed class PrototypeWorldConfig
{
    public string Id { get; set; }

    public List<string> CombatZones { get; set; } = new();

    public List<SpawnSlotDefinition> SpawnSlots { get; set; } = new();
}


public sealed class SpawnSlotDefinition
{
    public string Id { get; set; }

    public float PositionX { get; set; }

    public float PositionY { get; set; }

    public List<EnemyType> AllowedEnemies { get; set; } = new();
}


public sealed class EnemyRosterConfig
{
    public string Id { get; set; }

    public List<EnemyRosterEntry> Entries { get; set; } = new();
}


public sealed class EnemyRosterEntry
{
    public EnemyType EnemyType { get; set; }

    public string ScenePath { get; set; }

    public int ReserveCount { get; set; }

    public int MaxActive { get; set; }
}


public sealed class MusicPhraseConfig
{
    public string Id { get; set; }

    public string AudioPath { get; set; }

    public float Bpm { get; set; }

    public int BeatsPerBar { get; set; }

    public int BarsPerLoop { get; set; }

    public float LoopDurationSeconds { get; set; }

    public int Intensity { get; set; }

    public List<string> Tags { get; set; } = new();
}


public sealed class EncounterConfig
{
    public string Id { get; set; }

    public string WorldId { get; set; }

    public List<string> MusicPhraseIds { get; set; } = new();

    public List<EncounterSectionDefinition> Sections { get; set; } = new();
}


public sealed class EncounterSectionDefinition
{
    public string Id { get; set; }

    public int StartBar { get; set; }

    public int BarCount { get; set; }

    public int Intensity { get; set; }

    public ThreatType ThreatType { get; set; }

    public int BeatsBetweenThreats { get; set; }

    public float LeadTime { get; set; }
}
