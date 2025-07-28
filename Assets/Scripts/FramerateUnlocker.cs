using UnityEngine;

public class FramerateUnlocker : MonoBehaviour
{
    void Awake()
    {
        Debug.Log("FramerateUnlocker running");
        QualitySettings.vSyncCount = 0;        // disable VSync (prevents it from capping to display refresh rate)
        Application.targetFrameRate = 60;      // try to run at 60 FPS
    }
}
