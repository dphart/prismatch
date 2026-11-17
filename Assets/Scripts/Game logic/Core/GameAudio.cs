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
    private readonly Dictionary<int, AudioClip> rewardClips = new();
    public void PlayReward(float impact, int cascadeDepth, bool finish = false)
    {
        if (!Enabled || source == null) return;
        int tier = impact >= .25f ? 2 : impact >= .1f ? 1 : 0;
        int depth = Mathf.Clamp(cascadeDepth, 0, 6);
        int key = depth * 10 + tier + (finish ? 100 : 0);
        if (!rewardClips.TryGetValue(key, out var clip))
        {
            const int rate = 22050;
            float duration = finish ? .24f : .12f + tier * .045f;
            var samples = new float[Mathf.CeilToInt(rate * duration)];
            int[] notes = { 0, 2, 4, 7, 9, 12, 14 };
            float frequency = 440f * Mathf.Pow(2, notes[depth] / 12f);
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / rate;
                float envelope = Mathf.Min(1, t / .008f) * Mathf.Pow(1 - t / duration, 2);
                float wave = Mathf.Sin(2 * Mathf.PI * frequency * t);
                if (tier > 0 || finish) wave += .4f * Mathf.Sin(2 * Mathf.PI * frequency * 1.5f * t);
                if (tier > 1 || finish) wave += .25f * Mathf.Sin(2 * Mathf.PI * frequency * 2f * t);
                samples[i] = wave * envelope * .22f;
            }
            clip = AudioClip.Create("Reward_" + key, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            rewardClips[key] = clip;
        }
        source.PlayOneShot(clip, finish ? .35f : .55f);
    }

    private void OnDestroy()
    {
        foreach (var clip in clips.Values) if (clip != null) Destroy(clip);
        foreach (var clip in rewardClips.Values) if (clip != null) Destroy(clip);
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
