using UnityEngine;

/// <summary>
/// ContenaVolley の効果音。SoundManager に頼らずシーン単体で鳴らせるようにする。
/// 同時に鳴っても途切れないよう AudioSource を複数持ってローテーションする
/// </summary>
public static class CV_Audio
{
    const int Voices = 10;

    static AudioSource[] sources;
    static int next;

    public static float Volume = 0.8f;

    static void Init()
    {
        if (sources != null && sources[0] != null) return;
        var go = new GameObject("CV_Audio");
        sources = new AudioSource[Voices];
        for (int i = 0; i < Voices; i++)
        {
            sources[i] = go.AddComponent<AudioSource>();
            sources[i].playOnAwake = false;
        }
    }

    /// <summary>効果音を鳴らす。pitchJitter でピッチを少し揺らすと連打しても単調にならない</summary>
    public static void Play(string name, float volume = 1f, float pitch = 1f, float pitchJitter = 0.05f)
    {
        var clip = CV_Assets.Clip(name);
        if (clip == null) return;
        Init();

        var src = sources[next];
        next = (next + 1) % Voices;
        src.pitch = pitch + Random.Range(-pitchJitter, pitchJitter);
        src.PlayOneShot(clip, volume * Volume);
    }

    /// <summary>name1〜nameN のどれかをランダムに鳴らす</summary>
    public static void PlayRandom(string name, int variations, float volume = 1f, float pitch = 1f)
    {
        Play(name + Random.Range(1, variations + 1), volume, pitch);
    }
}
