using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MusicTrack
{
    public string title;
    public AudioClip clip;
    [Range(0f, 1f)] public float volumeScale = 1f;
}

public class MusicService : MonoBehaviour
{
    public static MusicService Instance { get; private set; }

    [Header("Playlista")]
    [SerializeField] private List<MusicTrack> _tracks = new List<MusicTrack>();
    [SerializeField] private bool _playOnStart = true;
    [SerializeField] private bool _shuffle = false;

    [Header("Audio")]
    [SerializeField] private AudioSource _source;
    [SerializeField, Range(0f, 1f)] private float _volume = 0.5f;
    [SerializeField] private float _fadeDuration = 0.5f;

    public event Action<MusicTrack> OnTrackChanged;
    public event Action<bool> OnPlayStateChanged;
    public event Action<float> OnVolumeChanged;

    private int _index = -1;
    private float _fadeTimer = -1f;
    private float _fadeFrom, _fadeTo;
    private bool _paused;

    public bool IsPlaying => _source && _source.isPlaying;
    public float Volume => _volume;
    public int TrackCount => _tracks.Count;
    public MusicTrack CurrentTrack => (_index >= 0 && _index < _tracks.Count) ? _tracks[_index] : null;

    public float Progress =>
        (_source && _source.clip && _source.clip.length > 0f)
            ? _source.time / _source.clip.length : 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (!_source) _source = GetComponent<AudioSource>();
        if (!_source) _source = gameObject.AddComponent<AudioSource>();

        _source.loop = false;
        _source.playOnAwake = false;
        _source.volume = TargetVolume();
    }

    private void Start()
    {
        OnVolumeChanged?.Invoke(_volume);

        if (_playOnStart && _tracks.Count > 0)
            PlayIndex(_shuffle ? RandomIndex() : 0);
    }

    private void Update()
    {
        UpdateFade();

        if (_source && _source.clip && !_source.isPlaying && !_paused
            && !AudioListener.pause && _index >= 0)
            Next();
    }

    /// <summary>Docelowa głośność z uwzględnieniem skali bieżącego utworu.</summary>
    private float TargetVolume()
    {
        var t = CurrentTrack;
        return _volume * (t != null ? t.volumeScale : 1f);
    }

    // ---------- sterowanie ----------

    public void PlayIndex(int i)
    {
        if (_tracks.Count == 0 || !_source) return;

        _index = Mathf.Clamp(i, 0, _tracks.Count - 1);
        var t = _tracks[_index];
        _paused = false;

        if (t.clip == null)
        {
            Debug.LogWarning($"MusicService: utwór '{t.title}' nie ma klipu.", this);
            return;
        }

        _source.clip = t.clip;

        if (_fadeDuration > 0f)
        {
            _source.volume = 0f;
            BeginFade(TargetVolume());
        }
        else
        {
            _source.volume = TargetVolume();
            _fadeTimer = -1f;
        }

        _source.Play();

        OnTrackChanged?.Invoke(t);
        OnPlayStateChanged?.Invoke(true);
    }

    public void Next()
    {
        if (_tracks.Count == 0) return;
        PlayIndex(_shuffle ? RandomIndex() : (_index + 1) % _tracks.Count);
    }

    public void Previous()
    {
        if (_tracks.Count == 0) return;
        if (_source && _source.time > 3f) { _source.time = 0f; return; }
        PlayIndex(_shuffle ? RandomIndex() : (_index - 1 + _tracks.Count) % _tracks.Count);
    }

    public void TogglePlay()
    {
        if (!_source) return;

        if (_source.isPlaying) Pause();
        else if (_source.clip) Resume();
        else if (_tracks.Count > 0) PlayIndex(0);   // nic nie było załadowane
    }

    public void Pause()
    {
        if (_source && _source.isPlaying)
        {
            _source.Pause();
            _paused = true;
            OnPlayStateChanged?.Invoke(false);
        }
    }

    public void Resume()
    {
        if (_source && !_source.isPlaying)
        {
            _source.UnPause();
            _paused = false;
            OnPlayStateChanged?.Invoke(true);
        }
    }

    public void SetVolume(float v)
    {
        _volume = Mathf.Clamp01(v);

        if (_source)
        {
            _fadeTimer = -1f;                  // ręczna zmiana przerywa fade
            _source.volume = TargetVolume();
        }

        OnVolumeChanged?.Invoke(_volume);
    }

    public void ChangeVolume(float delta) => SetVolume(_volume + delta);

    // ---------- fade ----------

    private void BeginFade(float to)
    {
        _fadeFrom = _source ? _source.volume : 0f;
        _fadeTo = to;
        _fadeTimer = 0f;
    }

    private void UpdateFade()
    {
        if (_fadeTimer < 0f || !_source) return;

        _fadeTimer += Time.deltaTime;
        float k = Mathf.Clamp01(_fadeTimer / Mathf.Max(0.01f, _fadeDuration));
        _source.volume = Mathf.Lerp(_fadeFrom, _fadeTo, k);

        if (k >= 1f) _fadeTimer = -1f;
    }

    private int RandomIndex()
    {
        if (_tracks.Count <= 1) return 0;

        int i;
        do { i = UnityEngine.Random.Range(0, _tracks.Count); } while (i == _index);
        return i;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying && _source) _source.volume = TargetVolume();
    }
#endif
}