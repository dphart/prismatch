using System.Collections.Generic;
using UnityEngine;

public enum GameSound { Swap, Clear, Power, Blocked, Warning, Victory }

/// <summary>Small synthesized cues so feedback works without imported audio assets.</summary>
public sealed class GameAudio : MonoBehaviour
{
    public bool Enabled { get; private set; } = true;
    private AudioSource source;
    private readonly Dictionary<GameSound, AudioClip> clips = new();
    private void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.ignoreListenerPause = true;
    }
    public void Play(GameSound sound)
    {
        if (!Enabled) return;
        if (!clips.TryGetValue(sound, out var clip)) clips[sound] = clip = Build(sound);
        source.PlayOneShot(clip, sound == GameSound.Warning ? .25f : .4f);
    }
    public void SetEnabled(bool enabled) => Enabled = enabled;
    private static AudioClip Build(GameSound sound)
    {
        const int rate = 22050;
        float seconds = sound == GameSound.Victory ? .7f : .13f;
        int length = Mathf.CeilToInt(rate * seconds);
        var data = new float[length];
        float start = sound switch { GameSound.Clear => 540, GameSound.Power => 720, GameSound.Blocked => 150,
            GameSound.Warning => 420, GameSound.Victory => 440, _ => 300 };
        float end = sound switch { GameSound.Blocked => 80, GameSound.Victory => 880, _ => start * 1.18f };
        for (int i = 0; i < length; i++)
        {
            float t = (float)i / length;
            float frequency = Mathf.Lerp(start, end, t);
            float envelope = Mathf.Sin(Mathf.PI * t) * Mathf.Lerp(1f, .2f, t);
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / rate) * envelope * .35f;
        }
        var clip = AudioClip.Create($"Prismatch_{sound}", length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
