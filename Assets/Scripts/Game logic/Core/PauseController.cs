using UnityEngine;

/// <summary>
/// Manages pause state for the timer and Time.timeScale.
/// </summary>
public class PauseController
{
    private readonly TimerController _timer;
    public bool IsPaused { get; private set; }

    public PauseController(TimerController timer)
    {
        _timer = timer;
    }

    public void TogglePause()
    {
        if (IsPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void Pause()
    {
        IsPaused = true;
        _timer?.Pause();
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        IsPaused = false;
        _timer?.Resume();
        Time.timeScale = 1f;
    }
}
