using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PomodoroSessionUI : MonoBehaviour
{
    [SerializeField] private timer_service _timer;
    [SerializeField] private DeskWorkstation _desk;
    [SerializeField] private FocusRewardPopup _rewardPopup;

    [Header("Obiekty")]
    [SerializeField] private GameObject _uiActive;
    [SerializeField] private GameObject _uiInactive;
    [SerializeField] private GameObject _breakObject;

    [Header("Wyświetlanie")]
    [SerializeField] private TMP_Text _timeTextActive;
    [SerializeField] private TMP_Text _timeTextInactive;
    [SerializeField] private Image _progressFill;
    [SerializeField] private bool _smoothProgress = true;

    [Header("Zakładki")]
    [SerializeField] private Button _workTab;
    [SerializeField] private Button _breakTab;
    [SerializeField] private Graphic _workTabGraphic;
    [SerializeField] private Graphic _breakTabGraphic;
    [SerializeField] private Color _tabActive = Color.white;
    [SerializeField] private Color _tabInactive = new Color(1f, 1f, 1f, 0.45f);
    [SerializeField] private TMP_Text _workTabText;
    [SerializeField] private TMP_Text _breakTabText;
    [SerializeField] private Color _tabTextActive = Color.white;
    [SerializeField] private Color _tabTextInactive = new Color(0.55f, 0.35f, 0.30f);

    [Header("Kolor treści (czas + ikony, bez zakładek)")]
    [Tooltip("Ikony play, pause, stop, strzałek. Podpinaj Image ikony, nie tło przycisku.")]
    [SerializeField] private Graphic[] _controlIcons;
    [SerializeField] private Color _contentWork = new Color(0.55f, 0.35f, 0.30f);
    [SerializeField] private Color _contentBreak = Color.white;
    [SerializeField] private bool _tintTimeText = true;

    [Header("UI_ACTIVE - timer chodzi")]
    [Tooltip("UI_TIMER_stop - PRZERYWA sesję. Timer do Idle, wartości do domyślnych, nagroda za przepracowany czas.")]
    [SerializeField] private Button _stopButton;
    [Tooltip("UI_TIMER_cancel - PAUZUJE. Czas zachowany, UI przechodzi w INACTIVE, bez nagrody.")]
    [SerializeField] private Button _pauseButton;

    [Header("UI_INACTIVE - ustawianie")]
    [Tooltip("UI_TIMER_play - start z Idle albo wznowienie po pauzie.")]
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _timeUpButton;
    [SerializeField] private Button _timeDownButton;

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
    [Tooltip("Klawisze działają tylko gdy gracz siedzi przy biurku.")]
    [SerializeField] private bool _requireSeated = true;

    [Header("Zachowanie")]
    [SerializeField] private bool _loop = true;
    [Tooltip("Czy PRZERWANIE (stop) przyznaje nagrodę za przepracowany czas.")]
    [SerializeField] private bool _rewardOnStop = true;

    private int _work, _brk;
    private bool _breakTabSelected;
    private float _phaseDuration = 1f;
    private bool _lastRunning;
    private float _holdTimer;
    private int _holdDir;

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
        RefreshTabs();
        RefreshValues();
        RefreshBreakObject();
    }

    // ================= AKCJE =================

    /// <summary>START / WZNOWIENIE. Z Idle startuje nową sesję, po pauzie wznawia bieżącą fazę.</summary>
    private void PlayOrResume()
    {
        if (_timer.phase == PomodoroPhase.Idle)
        {
            _timer.SetLoop(_loop);
            _timer.Configure(_work, _brk);

            if (_breakTabSelected) _timer.StartBreak();
            else                   _timer.StartWork();
        }
        else
        {
            _timer.Resume();
        }

        RefreshControls();
    }

    /// <summary>PAUZA. Zatrzymuje odliczanie, zachowuje pozostały czas. Bez nagrody.</summary>
    private void PauseSession()
    {
        if (!_timer.IsRunning) return;

        _timer.Pause();
        RefreshControls();
    }

    /// <summary>PRZERWANIE. Kończy sesję, resetuje wartości do domyślnych. Nagroda wg _rewardOnStop.</summary>
    private void StopSession()
    {
        if (_timer.phase == PomodoroPhase.Idle) return;

        if (!_rewardOnStop && _rewardPopup) _rewardPopup.SuppressNext();

        _timer.Stop();

        _work = _workDefault;
        _brk = _breakDefault;
        _breakTabSelected = false;
        _timer.Configure(_work, _brk);

        RefreshControls();
        RefreshTabs();
        RefreshValues();
        RefreshBreakObject();
    }

    // ================= zakładki i kolory =================

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

            RefreshTabs();
            RefreshValues();
            RefreshBreakObject();
            return;
        }

        // Idle - zakładka wybiera co edytujemy i czym wystartuje sesja
        _breakTabSelected = breakTab;
        RefreshTabs();
        RefreshValues();
        RefreshBreakObject();
    }

    private void RefreshTabs()
    {
        bool work = !_breakTabSelected;

        if (_workTabGraphic)  _workTabGraphic.color  = work ? _tabActive : _tabInactive;
        if (_breakTabGraphic) _breakTabGraphic.color = work ? _tabInactive : _tabActive;

        if (_workTabText)  _workTabText.color  = work ? _tabTextActive : _tabTextInactive;
        if (_breakTabText) _breakTabText.color = work ? _tabTextInactive : _tabTextActive;

        RefreshContentColor(work);
    }

    /// <summary>Kolor czasu i ikon sterujących - zakładki mają własny schemat.</summary>
    private void RefreshContentColor(bool work)
    {
        Color c = work ? _contentWork : _contentBreak;

        if (_tintTimeText)
        {
            if (_timeTextActive)   _timeTextActive.color = c;
            if (_timeTextInactive) _timeTextInactive.color = c;
        }

        if (_controlIcons == null) return;
        foreach (var g in _controlIcons)
            if (g) g.color = c;
    }

    private void RefreshBreakObject()
    {
        if (!_breakObject) return;

        bool onBreak = _timer.IsRunning
            ? _timer.phase == PomodoroPhase.Break
            : _breakTabSelected;

        _breakObject.SetActive(onBreak);
    }

    // ================= wartości =================

    private void Step(int dir)
    {
        bool midPhase = _timer.phase != PomodoroPhase.Idle;

        if (_breakTabSelected)
            _brk = Mathf.Clamp(_brk + dir * _breakStepSize, _breakMin, _breakMax);
        else
            _work = Mathf.Clamp(_work + dir * _workStepSize, _workMin, _workMax);

        _timer.Configure(_work, _brk);

        if (midPhase)
        {
            // przeładowanie fazy nową długością - to nie jest przerwanie sesji
            if (_rewardPopup) _rewardPopup.SuppressNext();

            if (_breakTabSelected) _timer.StartBreak();
            else                   _timer.StartWork();
            _timer.Pause();
        }

        RefreshValues();
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

    // ================= pętla =================

    private void Update()
    {
        if (!_timer) return;

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

    // ================= reakcja na timer =================

    private void HandlePhaseChange(PomodoroPhase phase)
    {
        _phaseDuration = Mathf.Max(0.01f, _timer.PhaseDuration);

        if (phase != PomodoroPhase.Idle)
        {
            _breakTabSelected = phase == PomodoroPhase.Break;
            RefreshTabs();
            RefreshValues();
        }

        RefreshBreakObject();
    }

    private void HandleTick(float remaining)
    {
        if (_timeTextActive) _timeTextActive.text = _timer.TimeString;

        if (!_smoothProgress && _progressFill)
            _progressFill.fillAmount = 1f - Mathf.Clamp01(remaining / _phaseDuration);
    }

    private void HandleFinished()
    {
        RefreshControls();
        RefreshValues();
        RefreshBreakObject();
    }

    private void RefreshControls()
    {
        _lastRunning = _timer.IsRunning;
        if (_uiActive)   _uiActive.SetActive(_lastRunning);
        if (_uiInactive) _uiInactive.SetActive(!_lastRunning);
        _holdDir = 0;

        RefreshBreakObject();
    }
}