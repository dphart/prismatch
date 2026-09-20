using System;

/// <summary>
/// Handles countdown timing, pause state, and expiration events.
/// Pure C# class so it can be unit tested without MonoBehaviour.
/// </summary>
public class TimerController
{
    public float Duration { get; private set; }
    public float Remaining { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsPaused { get; private set; }

    public event Action<float> OnTick;
    public event Action OnExpired;

    public void Reset(float durationSeconds)
    {
        Duration = durationSeconds;
        Remaining = durationSeconds;
        IsRunning = false;
        IsPaused = false;
        NotifyTick();
    }

    public void Start()
    {
        if (Remaining <= 0f) return;
        IsRunning = true;
        IsPaused = false;
    }

    public void Stop()
    {
        IsRunning = false;
    }

    public void Pause()
    {
        IsPaused = true;
    }

    public void Resume()
    {
        IsPaused = false;
    }

    public void TogglePause()
    {
        IsPaused = !IsPaused;
    }

    public void ApplyPenalty(float seconds)
    {
        Remaining = Math.Max(0f, Remaining - Math.Max(0f, seconds));
        NotifyTick();
        CheckExpired();
    }

    public void Tick(float deltaTime)
    {
        if (!IsRunning || IsPaused) return;

        Remaining = Math.Max(0f, Remaining - Math.Max(0f, deltaTime));
        NotifyTick();
        CheckExpired();
    }

    private void NotifyTick()
    {
        OnTick?.Invoke(Remaining);
    }

    private void CheckExpired()
    {
        if (Remaining <= 0f && IsRunning)
        {
            IsRunning = false;
            OnExpired?.Invoke();
        }
    }
}
