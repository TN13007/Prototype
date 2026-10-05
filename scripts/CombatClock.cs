using System;
using Godot;


public partial class CombatClock : Node
{
    public float Time { get; private set; }

    public bool IsRunning { get; private set; }

    public event Action<float> Advanced;

    private AudioStreamPlayer audioSource;

    private float loopDuration;

    private float completedLoopTime;


    public override void _Ready()
    {
        AddToGroup("combat_clock");
    }


    public override void _Process(double delta)
    {
        if(!IsRunning)
            return;

        if(audioSource != null && audioSource.Playing)
        {
            Time = completedLoopTime +
                   audioSource.GetPlaybackPosition();
        }
        else
        {
            Time += (float)delta;
        }

        Advanced?.Invoke(Time);
    }


    public void Start(float startTime = 0f)
    {
        Time = Mathf.Max(startTime, 0f);
        IsRunning = true;
    }


    public void Stop()
    {
        IsRunning = false;
    }


    public void Reset(float startTime = 0f)
    {
        Time = Mathf.Max(startTime, 0f);
        completedLoopTime = Time;
    }


    public void BindAudio(
        AudioStreamPlayer source,
        float audioLoopDuration)
    {
        audioSource = source;
        loopDuration = Mathf.Max(audioLoopDuration, 0f);
        completedLoopTime = 0f;
    }


    public void NotifyAudioLoop()
    {
        if(loopDuration <= 0f)
            return;

        completedLoopTime += loopDuration;
    }
}
