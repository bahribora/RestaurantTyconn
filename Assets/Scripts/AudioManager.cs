using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Boş bırakırsan kod kendi sesini üretir")]
    public AudioClip CoinClip;
    public AudioClip AngryClip;
    public AudioClip BuyClip;
    public AudioClip DayEndClip;
    public AudioClip MusicClip;

    public float SfxVolume = 0.6f;
    public float MusicVolume = 0.2f;

    const int Rate = 44100;
    AudioSource sfx;
    AudioSource music;

    public bool MusicOn { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        sfx = gameObject.AddComponent<AudioSource>();
        music = gameObject.AddComponent<AudioSource>();
        music.loop = true;
        music.volume = MusicVolume;

        if (CoinClip == null) CoinClip = MakeSequence("coin", new float[] { 988f, 1319f }, 0.12f, 0.3f);
        if (AngryClip == null) AngryClip = MakeSequence("angry", new float[] { 196f, 147f }, 0.2f, 0.8f);
        if (BuyClip == null) BuyClip = MakeSequence("buy", new float[] { 660f, 880f }, 0.08f, 0.3f);
        if (DayEndClip == null) DayEndClip = MakeSequence("dayend", new float[] { 523f, 659f, 784f, 1047f }, 0.2f, 0.3f);
        if (MusicClip == null) MusicClip = MakeMusic();

        music.clip = MusicClip;
    }

    void Start()
    {
        MusicOn = PlayerPrefs.GetInt("music", 1) == 1;
        ApplyMusic();
    }

    void ApplyMusic()
    {
        music.volume = MusicVolume;
        if (MusicOn)
        {
            if (!music.isPlaying) music.Play();
        }
        else
        {
            music.Stop();
        }
    }

    public void ToggleMusic()
    {
        MusicOn = !MusicOn;
        PlayerPrefs.SetInt("music", MusicOn ? 1 : 0);
        ApplyMusic();
    }

    public void PlayCoin() { sfx.PlayOneShot(CoinClip, SfxVolume); }
    public void PlayAngry() { sfx.PlayOneShot(AngryClip, SfxVolume); }
    public void PlayBuy() { sfx.PlayOneShot(BuyClip, SfxVolume); }
    public void PlayDayEnd() { sfx.PlayOneShot(DayEndClip, SfxVolume); }

    AudioClip MakeSequence(string clipName, float[] freqs, float noteLen, float harmonic)
    {
        int perNote = (int)(Rate * noteLen);
        float[] data = new float[perNote * freqs.Length];

        for (int n = 0; n < freqs.Length; n++)
        {
            for (int i = 0; i < perNote; i++)
            {
                float t = i / (float)Rate;
                float w = 2f * Mathf.PI * freqs[n] * t;
                float s = (Mathf.Sin(w) + harmonic * Mathf.Sin(2f * w)) / (1f + harmonic);
                float env = Mathf.Pow(1f - (float)i / perNote, 2f) * Mathf.Min(1f, i / 100f);
                data[n * perNote + i] = s * env * 0.5f;
            }
        }

        AudioClip clip = AudioClip.Create(clipName, data.Length, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    AudioClip MakeMusic()
    {
        float[] melody =
        {
            523.25f, 659.25f, 783.99f, 659.25f,
            587.33f, 783.99f, 880.00f, 783.99f,
            523.25f, 659.25f, 783.99f, 1046.50f,
            880.00f, 783.99f, 659.25f, 587.33f
        };
        float[] bass = { 130.81f, 146.83f, 174.61f, 146.83f };

        float step = 0.4f;
        int perStep = (int)(Rate * step);
        float[] data = new float[perStep * melody.Length];

        for (int n = 0; n < melody.Length; n++)
        {
            float bassFreq = bass[n / 4];
            for (int i = 0; i < perStep; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Pow(1f - (float)i / perStep, 1.5f) * Mathf.Min(1f, i / 200f);
                float m = Mathf.Sin(2f * Mathf.PI * melody[n] * t) * env * 0.25f;

                float bt = ((n % 4) * perStep + i) / (float)Rate;
                float bpos = ((n % 4) * perStep + i) / (float)(4 * perStep);
                float benv = Mathf.Min(1f, bpos * 20f) * Mathf.Min(1f, (1f - bpos) * 20f);
                float b = Mathf.Sin(2f * Mathf.PI * bassFreq * bt) * benv * 0.2f;

                data[n * perStep + i] = m + b;
            }
        }

        AudioClip clip = AudioClip.Create("music", data.Length, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}