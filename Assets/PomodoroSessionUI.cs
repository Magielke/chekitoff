using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PomodoroSessionUI : MonoBehaviour
{
    private class TintEntry
    {
        public Graphic graphic;
        public Color baseColor;
        public Color from, to;
    }

    [SerializeField] private timer_service _timer;
    [SerializeField] private DeskWorkstation _desk;
    [SerializeField] private FocusRewardPopup _rewardPopup;

    [Header("Obiekty")]
    [SerializeField] private GameObject _uiActive;
    [SerializeField] private GameObject _uiInactive;
    [SerializeField] private GameObject _breakObject;
    [Tooltip("CanvasGroup na _breakObject - potrzebny do płynnego pojawiania się.")]
    [SerializeField] private CanvasGroup _breakObjectGroup;

    [Header("Wyświetlanie")]
    [SerializeField] private TMP_Text _timeTextActive;
    [SerializeField] private TMP_Text _timeTextInactive;
    [SerializeField] private Image _progressFill;
    [SerializeField] private bool _smoothProgress = true;

    [Header("Kolory - automatyczne wykrywanie")]
    [Tooltip("Korzenie, z których zbierane są Graphic. Puste = ten obiekt.")]
    [SerializeField] private Transform[] _tintRoots;
    [Tooltip("Pomijane przy zbieraniu - zakładki, tła, elementy o stałym kolorze.")]
    [SerializeField] private Graphic[] _tintExclude;
    [Tooltip("Kolor w trybie BREAK. Alpha każdego elementu zostaje własna.")]
    [SerializeField] private Color _breakColor = Color.white;
    [SerializeField] private float _tintFadeDuration = 0.35f;

    [Header("Zakładki")]
    [SerializeField] private Button _workTab;
    [SerializeField] private Button _breakTab;

    [Header("UI_ACTIVE - timer chodzi")]
    [Tooltip("UI_TIMER_stop - PRZERYWA sesję. Timer do Idle, wartości do domyślnych, nagroda.")]
    [SerializeField] private Button _stopButton;
    [Tooltip("UI_TIMER_cancel - PAUZUJE. Czas zachowany, bez nagrody.")]
    [SerializeField] private Button _pauseButton;

    [Header("UI_INACTIVE - ustawianie")]
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _timeUpButton;
    [SerializeField] private Button _timeDownButton;

    [Header("Dźwięk")]
    [Tooltip("Osobny AudioSource, nie ten od muzyki.")]
    [SerializeField] private AudioSource _sfxSource;
    [SerializeField] private AudioClip _focusEndClip;
    [SerializeField] private AudioClip _breakEndClip;
    [SerializeField, Range(0f, 1f)] private float _sfxVolume = 0.8f;

    [Header("Zakresy - praca (minuty)")]
    [SerializeField] private int _workDefault = 25;
    [SerializeField] private int _workMin = 1;
    [SerializeField] private int _workMax = 120;
    [SerializeField] private int _workStepSize = 5;

    [Header("Zakresy - przerwa (minuty)")]
    [SerializeField] private int _breakDefault = 5;
    [SerializeField] private int _breakMin = 1;
    [SerializeField] private int _breakMax = 60;
    [SerializeField] private int _breakStepSize = 1;

    [Header("Klawisze")]
    [SerializeField] private KeyCode _upKey = KeyCode.UpArrow;
    [SerializeField] private KeyCode _downKey = KeyCode.DownArrow;
    [SerializeField] private KeyCode _tabKey = KeyCode.Tab;
    [SerializeField] private KeyCode _confirmKey = KeyCode.Return;
    [SerializeField] private float _repeatDelay = 0.4f;
    [SerializeField] private float _repeatRate = 0.08f;

    [Header("Dostępność")]
    [SerializeField] private bool _requireSeated = true;

    [Header("Zachowanie")]
    [SerializeField] private bool _loop = true;
    [Tooltip("Czy PRZERWANIE (stop) przyznaje nagrodę za przepracowany czas.")]
    [SerializeField] private bool _rewardOnStop = true;

    private readonly List<TintEntry> _tint = new List<TintEntry>();

    private int _work, _brk;
    private bool _breakTabSelected;
    private float _phaseDuration = 1f;
    private bool _lastRunning;
    private float _holdTimer;
    private int _holdDir;

    private float _tintTimer = -1f;
    private float _breakAlphaFrom, _breakAlphaTo;
    private bool _sfxPlayedThisPhase;

    private void Awake()
    {
        _work = _workDefault;
        _brk = _breakDefault;

        if (_stopButton)  _stopButton.onClick.AddListener(StopSession);
        if (_pauseButton) _pauseButton.onClick.AddListener(PauseSession);

        if (_playButton)     _playButton.onClick.AddListener(PlayOrResume);
        if (_timeUpButton)   _timeUpButton.onClick.AddListener(() => Step(+1));
        if (_timeDownButton) _timeDownButton.onClick.AddListener(() => Step(-1));

        if (_workTab)  _workTab.onClick.AddListener(() => SelectTab(false));
        if (_breakTab) _breakTab.onClick.AddListener(() => SelectTab(true));

        CaptureBaseColors();

        if (_timer)
        {
            _timer.OnPhaseChange += HandlePhaseChange;
            _timer.OnTick += HandleTick;
            _timer.OnFinished += HandleFinished;
        }
    }

    private void OnDestroy()
    {
        if (_timer)
        {
            _timer.OnPhaseChange -= HandlePhaseChange;
            _timer.OnTick -= HandleTick;
            _timer.OnFinished -= HandleFinished;
        }
    }

    private void Start()
    {
        if (!_timer)
        {
            Debug.LogError("PomodoroSessionUI: brak referencji do timer_service.", this);
            enabled = false;
            return;
        }

        _timer.Configure(_work, _brk);
        RefreshControls();
        ApplyTintInstant();
        RefreshValues();
    }

    // ================= KOLORY =================

    /// <summary>Zbiera wszystkie Graphic z korzeni i zapamiętuje kolor bazowy każdego z osobna.</summary>
    private void CaptureBaseColors()
    {
        _tint.Clear();

        Transform[] roots = (_tintRoots != null && _tintRoots.Length > 0)
            ? _tintRoots
            : new[] { transform };

        foreach (var root in roots)
        {
            if (!root) continue;

            foreach (var g in root.GetComponentsInChildren<Graphic>(true))
            {
                if (IsExcluded(g)) continue;
                if (_tint.Exists(e => e.graphic == g)) continue;   // korzenie mogą się nakładać

                _tint.Add(new TintEntry
                {
                    graphic = g,
                    baseColor = g.color,
                    from = g.color,
                    to = g.color
                });
            }
        }
    }

    private bool IsExcluded(Graphic g)
    {
        if (!g) return true;
        if (g == _progressFill) return true;

        // zakładki mają własny schemat
        if (_workTab && g.transform.IsChildOf(_workTab.transform)) return true;
        if (_breakTab && g.transform.IsChildOf(_breakTab.transform)) return true;

        // obiekt przerwy steruje się alphą, nie kolorem
        if (_breakObject && g.transform.IsChildOf(_breakObject.transform)) return true;

        if (_tintExclude != null)
            foreach (var e in _tintExclude)
                if (e == g) return true;

        return false;
    }

    private void BeginTint(bool toBreak)
    {
        foreach (var t in _tint)
        {
            if (!t.graphic) continue;
            t.from = t.graphic.color;
            t.to = toBreak ? WithAlpha(_breakColor, t.baseColor.a) : t.baseColor;
        }

        _breakAlphaFrom = _breakObjectGroup ? _breakObjectGroup.alpha : (toBreak ? 0f : 1f);
        _breakAlphaTo = toBreak ? 1f : 0f;

        if (toBreak && _breakObject) _breakObject.SetActive(true);

        _tintTimer = 0f;
    }

    private void ApplyTintInstant()
    {
        foreach (var t in _tint)
        {
            if (!t.graphic) continue;
            t.to = _breakTabSelected ? WithAlpha(_breakColor, t.baseColor.a) : t.baseColor;
            t.from = t.to;
            t.graphic.color = t.to;
        }

        if (_breakObject) _breakObject.SetActive(_breakTabSelected);
        if (_breakObjectGroup) _breakObjectGroup.alpha = _breakTabSelected ? 1f : 0f;

        _tintTimer = -1f;
    }

    private void UpdateTint()
    {
        if (_tintTimer < 0f) return;

        _tintTimer += Time.deltaTime;
        float k = Mathf.Clamp01(_tintTimer / Mathf.Max(0.01f, _tintFadeDuration));
        float s = Mathf.SmoothStep(0f, 1f, k);

        foreach (var t in _tint)
        {
            if (!t.graphic) continue;
            t.graphic.color = Color.Lerp(t.from, t.to, s);
        }

        if (_breakObjectGroup)
            _breakObjectGroup.alpha = Mathf.Lerp(_breakAlphaFrom, _breakAlphaTo, s);

        if (k >= 1f)
        {
            _tintTimer = -1f;
            if (_breakObject && _breakAlphaTo <= 0f) _breakObject.SetActive(false);
        }
    }

    private static Color WithAlpha(Color c, float a)
    {
        c.a = a;
        return c;
    }

    // ================= AKCJE =================

    /// <summary>START / WZNOWIENIE.</summary>
    private void PlayOrResume()
    {
        if (_timer.phase == PomodoroPhase.Idle)
        {
            _timer.SetLoop(_loop);
            _timer.Configure(_work, _brk);
            _sfxPlayedThisPhase = false;

            if (_breakTabSelected) _timer.StartBreak();
            else                   _timer.StartWork();
        }
        else
        {
            _timer.Resume();
        }

        RefreshControls();
    }

    /// <summary>PAUZA - czas zachowany, bez nagrody.</summary>
    private void PauseSession()
    {
        if (!_timer.IsRunning) return;

        _timer.Pause();
        RefreshControls();
    }

    /// <summary>PRZERWANIE - koniec sesji, reset wartości, nagroda wg _rewardOnStop.</summary>
    private void StopSession()
    {
        if (_timer.phase == PomodoroPhase.Idle) return;

        if (!_rewardOnStop && _rewardPopup) _rewardPopup.SuppressNext();

        _timer.Stop();

        _work = _workDefault;
        _brk = _breakDefault;

        bool wasBreak = _breakTabSelected;
        _breakTabSelected = false;
        _timer.Configure(_work, _brk);

        RefreshControls();
        if (wasBreak) BeginTint(false);
        RefreshValues();
    }

    // ================= ZAKŁADKI =================

    private void SelectTab(bool breakTab)
    {
        _holdDir = 0;

        // timer chodzi - skip do wybranej fazy
        if (_timer.IsRunning)
        {
            if (breakTab && _timer.phase != PomodoroPhase.Break) _timer.StartBreak();
            else if (!breakTab && _timer.phase != PomodoroPhase.Work) _timer.StartWork();
            return;
        }

        bool changed = _breakTabSelected != breakTab;

        // pauza w trakcie fazy - przełącz fazę, zostań zapauzowany
        if (_timer.phase != PomodoroPhase.Idle)
        {
            _breakTabSelected = breakTab;

            if (breakTab && _timer.phase != PomodoroPhase.Break)
            {
                if (_rewardPopup) _rewardPopup.SuppressNext();
                _timer.StartBreak();
                _timer.Pause();
            }
            else if (!breakTab && _timer.phase != PomodoroPhase.Work)
            {
                _timer.StartWork();
                _timer.Pause();
            }

            if (changed) BeginTint(breakTab);
            RefreshValues();
            return;
        }

        // Idle - zakładka wybiera co edytujemy i czym wystartuje sesja
        _breakTabSelected = breakTab;
        if (changed) BeginTint(breakTab);
        RefreshValues();
    }

    // ================= WARTOŚCI =================

    private void Step(int dir)
    {
        bool midPhase = _timer.phase != PomodoroPhase.Idle;

        if (_breakTabSelected)
            _brk = StepValue(_brk, dir, _breakStepSize, _breakMin, _breakMax);
        else
            _work = StepValue(_work, dir, _workStepSize, _workMin, _workMax);

        _timer.Configure(_work, _brk);

        if (midPhase)
        {
            if (_rewardPopup) _rewardPopup.SuppressNext();

            if (_breakTabSelected) _timer.StartBreak();
            else                   _timer.StartWork();
            _timer.Pause();
        }

        RefreshValues();
    }

    /// <summary>Snapuje do wielokrotności kroku: przy min=1, step=5 daje 1 → 5 → 10.</summary>
    private int StepValue(int current, int dir, int step, int min, int max)
    {
        if (step <= 0) step = 1;

        int next;
        if (dir > 0)
        {
            next = ((current / step) + 1) * step;
        }
        else
        {
            next = ((current - 1) / step) * step;
            if (next < step) next = min;
        }

        return Mathf.Clamp(next, min, max);
    }

    private void RefreshValues()
    {
        if (!_timeTextInactive) return;

        if (_timer.phase != PomodoroPhase.Idle)
        {
            _timeTextInactive.text = _timer.TimeString;
            return;
        }

        int v = _breakTabSelected ? _brk : _work;
        _timeTextInactive.text = $"{v:D2}:00";
    }

    // ================= PĘTLA =================

    private void Update()
    {
        if (!_timer) return;

        UpdateTint();

        if (_timer.IsRunning != _lastRunning) RefreshControls();

        if (_timer.IsRunning)
        {
            if (_smoothProgress && _progressFill)
                _progressFill.fillAmount = 1f - Mathf.Clamp01(_timer.RemainingTime / _phaseDuration);
            return;
        }

        if (!KeyboardAvailable()) return;

        if (Input.GetKeyDown(_tabKey)) SelectTab(!_breakTabSelected);

        if (Input.GetKeyDown(_upKey))   BeginHold(+1);
        if (Input.GetKeyDown(_downKey)) BeginHold(-1);
        if (Input.GetKeyUp(_upKey)   && _holdDir > 0) _holdDir = 0;
        if (Input.GetKeyUp(_downKey) && _holdDir < 0) _holdDir = 0;

        if (_holdDir != 0)
        {
            _holdTimer -= Time.unscaledDeltaTime;
            if (_holdTimer <= 0f)
            {
                Step(_holdDir);
                _holdTimer = _repeatRate;
            }
        }

        if (Input.GetKeyDown(_confirmKey)) PlayOrResume();
    }

    /// <summary>Klawisze działają tylko gdy start timera jest realnie możliwy.</summary>
    private bool KeyboardAvailable()
    {
        if (TodoItemUI.AnyEditing) return false;
        if (NamePromptPopup.IsOpen || FocusRewardPopup.IsOpen) return false;
        if (_requireSeated && _desk && !_desk.IsSeated) return false;
        if (_uiInactive && !_uiInactive.activeInHierarchy) return false;

        return true;
    }

    private void BeginHold(int dir)
    {
        _holdDir = dir;
        _holdTimer = _repeatDelay;
        Step(dir);
    }

    // ================= REAKCJA NA TIMER =================

    private void HandlePhaseChange(PomodoroPhase phase)
    {
        _phaseDuration = Mathf.Max(0.01f, _timer.PhaseDuration);
        _sfxPlayedThisPhase = false;

        if (phase != PomodoroPhase.Idle)
        {
            bool wasBreak = _breakTabSelected;
            _breakTabSelected = phase == PomodoroPhase.Break;

            if (wasBreak != _breakTabSelected) BeginTint(_breakTabSelected);
            RefreshValues();
        }
    }

    private void HandleTick(float remaining)
    {
        if (_timeTextActive) _timeTextActive.text = _timer.TimeString;

        if (!_smoothProgress && _progressFill)
            _progressFill.fillAmount = 1f - Mathf.Clamp01(remaining / _phaseDuration);

        if (remaining <= 0f) PlayPhaseEndSfx();
    }

    private void PlayPhaseEndSfx()
    {
        if (_sfxPlayedThisPhase || !_sfxSource) return;
        _sfxPlayedThisPhase = true;

        AudioClip clip = _timer.phase == PomodoroPhase.Work ? _focusEndClip : _breakEndClip;
        if (clip) _sfxSource.PlayOneShot(clip, _sfxVolume);
    }

    private void HandleFinished()
    {
        RefreshControls();
        RefreshValues();
    }

    private void RefreshControls()
    {
        _lastRunning = _timer.IsRunning;
        if (_uiActive)   _uiActive.SetActive(_lastRunning);
        if (_uiInactive) _uiInactive.SetActive(!_lastRunning);
        _holdDir = 0;
    }
}