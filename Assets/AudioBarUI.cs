using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AudioBarUI : MonoBehaviour
{
    [SerializeField] private MusicService _music;

    [Header("Wyświetlanie")]
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private Image _progressFill;
    [SerializeField] private Image _volumeFill;

    [Header("Przyciski")]
    [SerializeField] private Button _prevButton;
    [SerializeField] private Button _playPauseButton;
    [SerializeField] private Button _nextButton;

    [Header("Ikony play/pause")]
    [SerializeField] private Image _playPauseIcon;
    [SerializeField] private Sprite _playSprite;
    [SerializeField] private Sprite _pauseSprite;

    [Header("Klawisze")]
    [SerializeField] private KeyCode _prevKey = KeyCode.Comma;
    [SerializeField] private KeyCode _nextKey = KeyCode.Period;
    [SerializeField] private KeyCode _toggleKey = KeyCode.M;
    [SerializeField] private KeyCode _volUpKey = KeyCode.Equals;
    [SerializeField] private KeyCode _volDownKey = KeyCode.Minus;
    [SerializeField] private float _volumeStep = 0.1f;

    private void Awake()
    {
        if (_prevButton)      _prevButton.onClick.AddListener(() => _music.Previous());
        if (_nextButton)      _nextButton.onClick.AddListener(() => _music.Next());
        if (_playPauseButton) _playPauseButton.onClick.AddListener(() => _music.TogglePlay());

        if (_music)
        {
            _music.OnTrackChanged += HandleTrackChanged;
            _music.OnPlayStateChanged += HandlePlayStateChanged;
            _music.OnVolumeChanged += HandleVolumeChanged;
        }
    }

    private void OnDestroy()
    {
        if (_music)
        {
            _music.OnTrackChanged -= HandleTrackChanged;
            _music.OnPlayStateChanged -= HandlePlayStateChanged;
            _music.OnVolumeChanged -= HandleVolumeChanged;
        }
    }

    private void Start()
    {
        if (!_music)
        {
            Debug.LogError("AudioBarUI: brak referencji do MusicService.", this);
            enabled = false;
            return;
        }

        HandleTrackChanged(_music.CurrentTrack);
        HandlePlayStateChanged(_music.IsPlaying);
        HandleVolumeChanged(_music.Volume);
    }

    private void Update()
    {
        if (!_music) return;
        if (TodoItemUI.AnyEditing || NamePromptPopup.IsOpen || FocusRewardPopup.IsOpen) return;

        if (Input.GetKeyDown(_prevKey))    _music.Previous();
        if (Input.GetKeyDown(_nextKey))    _music.Next();
        if (Input.GetKeyDown(_toggleKey))  _music.TogglePlay();
        if (Input.GetKeyDown(_volUpKey))   _music.ChangeVolume(+_volumeStep);
        if (Input.GetKeyDown(_volDownKey)) _music.ChangeVolume(-_volumeStep);

        if (_progressFill) _progressFill.fillAmount = _music.Progress;
    }

    private void HandleTrackChanged(MusicTrack t)
    {
        if (_titleText) _titleText.text = t != null ? t.title : "";
    }

    private void HandlePlayStateChanged(bool playing)
    {
        if (!_playPauseIcon) return;

        Sprite s = playing ? _pauseSprite : _playSprite;
        if (s) _playPauseIcon.sprite = s;
    }

    private void HandleVolumeChanged(float v)
    {
        if (_volumeFill) _volumeFill.fillAmount = v;
    }
}