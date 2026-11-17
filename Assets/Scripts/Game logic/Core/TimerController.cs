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

    private readonly System.Collections.Generic.HashSet<object> presentationHolds = new();
    public bool IsPresentationHeld => presentationHolds.Count > 0;
    public IDisposable HoldPresentation()
    {
        var key = new object();presentationHolds.Add(key);
        return new PresentationHold(() => presentationHolds.Remove(key));
    }
    private sealed class PresentationHold : IDisposable
    {
        private Action release;
        public PresentationHold(Action release) { this.release=release; }
        public void Dispose() { var action=release;release=null;action?.Invoke(); }
    }
    public void ClearPresentationHolds() => presentationHolds.Clear();

    public event Action<float> OnTick;
    public event Action OnExpired;

    public void Reset(float durationSeconds)
    {
        ClearPresentationHolds();
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

    public void AddTime(float seconds)
    {
        // A reward cannot revive an expired timer or alter its configured duration.
        if (!IsRunning || Remaining <= 0f || seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
        Remaining = Math.Min(float.MaxValue, Remaining + seconds);
        NotifyTick();
    }

    public void Tick(float deltaTime)
    {
        if (!IsRunning || IsPaused || IsPresentationHeld) return;

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
