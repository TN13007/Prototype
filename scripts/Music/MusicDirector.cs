using Godot;


public partial class MusicDirector : Node
{
    private AudioStreamPlayer player;

    private CombatClock clock;

    private MusicPhraseConfig currentPhrase;


    public override void _Ready()
    {
        player = GetNodeOrNull<AudioStreamPlayer>("AudioStreamPlayer");

        if(player != null)
            player.Finished += OnPhraseFinished;
    }


    public bool Play(
        MusicPhraseConfig phrase,
        CombatClock combatClock)
    {
        if(phrase == null || combatClock == null || player == null)
        {
            GD.PushError("MusicDirector requires a phrase, CombatClock and AudioStreamPlayer.");
            return false;
        }

        AudioStream stream = ResourceLoader.Load<AudioStream>(phrase.AudioPath);

        if(stream == null)
        {
            GD.PushError($"Could not load music stream: {phrase.AudioPath}");
            return false;
        }

        currentPhrase = phrase;
        clock = combatClock;
        player.Stream = stream;
        clock.BindAudio(player, phrase.LoopDurationSeconds);
        player.Play();
        clock.Start();

        GD.Print($"MUSIC: Playing phrase {phrase.Id} at {phrase.Bpm} BPM");
        return true;
    }


    private void OnPhraseFinished()
    {
        if(currentPhrase == null || player == null)
            return;

        clock?.NotifyAudioLoop();
        player.Play();
    }
}
