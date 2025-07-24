using UnityEngine;

public interface TimeProvider 
{
  public int TimeRemaining {  get; }
    public int TimeElapsed {  get; }
}
