using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PomodoroSessionUI : MonoBehaviour
{
    [SerializeField] private timer_service _timer;
    [SerializeField] private DeskWorkstation _desk;

    [Header("Obiekty")]
    [SerializeField] private GameObject _uiActive;      // UI_ACTIVE - timer chodzi
    [SerializeField] private GameObject _uiInactive;    // UI_INACTIVE - ustawianie
    [SerializeField] private GameObject _breakObject;   // tło przerwy

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

    [Header("Przyciski - UI_ACTIVE")]
    [SerializeField] private Button _stopButton;        // pauza
    [SerializeField] private Button _cancelButton;      // anuluj sesję

    [Header("Przyciski - UI_INACTIVE")]
    [SerializeField] private Button _timeUpButton;
    [SerializeField] private Button _timeDownButton;
    [SerializeField] private Button _playButton;

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

    [Header("Pętla")]
    [SerializeField] private bool _loop = true;

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

        if (_stopButton)   _stopButton.onClick.AddListener(PauseTimer);
        if (_cancelButton) _cancelButton.onClick.AddListener(CancelTimer);

        if (_playButton)     _playButton.onClick.AddListener(PlayTimer);
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
        _timer.Configure(_work, _brk);
        RefreshControls();
        RefreshTabs();
        RefreshValues();
        RefreshBreakObject();
    }

    // ---------- zakładki ----------

    private void SelectTab(bool breakTab)
    {
        _holdDir = 0;

        // timer chodzi - skip do tej fazy
        if (_timer.IsRunning)
        {
            if (breakTab && _timer.phase != PomodoroPhase.Break) _timer.StartBreak();
            else if (!breakTab && _timer.phase != PomodoroPhase.Work) _timer.StartWork();
            return;
        }

        // pauza w trakcie fazy - przełącz fazę i zostań zapauzowany
        if (_timer.phase != PomodoroPhase.Idle)
        {
            _breakTabSelected = breakTab;

            if (breakTab && _timer.phase != PomodoroPhase.Break) { _timer.StartBreak(); _timer.Pause(); }
            else if (!breakTab && _timer.phase != PomodoroPhase.Work) { _timer.StartWork(); _timer.Pause(); }

            RefreshTabs();
            RefreshValues();
            RefreshBreakObject();
            return;
        }

        // Idle - zakładka wybiera co edytujemy
        _breakTabSelected = breakTab;
        RefreshTabs();
        RefreshValues();
        RefreshBreakObject();
    }

    private void RefreshTabs()
    {
        if (_workTabGraphic)  _workTabGraphic.color  = _breakTabSelected ? _tabInactive : _tabActive;
        if (_breakTabGraphic) _breakTabGraphic.color = _breakTabSelected ? _tabActive : _tabInactive;
    }

    private void RefreshBreakObject()
    {
        if (!_breakObject) return;

        bool onBreak = _timer.IsRunning
            ? _timer.phase == PomodoroPhase.Break
            : _breakTabSelected;

        _breakObject.SetActive(onBreak);
    }

    // ---------- sterowanie ----------

    private void PlayTimer()
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

    private void PauseTimer()
    {
        _timer.Pause();
        RefreshControls();
    }

    /// <summary>Anuluje sesję - timer do Idle, wartości do domyślnych.</summary>
    private void CancelTimer()
    {
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

    // ---------- wartości ----------

    private void Step(int dir)
    {
        // w pauzie strzałki startują fazę od nowa z nową wartością
        bool midPhase = _timer.phase != PomodoroPhase.Idle;

        if (_breakTabSelected)
            _brk = Mathf.Clamp(_brk + dir * _breakStepSize, _breakMin, _breakMax);
        else
            _work = Mathf.Clamp(_work + dir * _workStepSize, _workMin, _workMax);

        _timer.Configure(_work, _brk);

        if (midPhase)
        {
            // przeładuj bieżącą fazę nową długością i zostaw zapauzowaną
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

    // ---------- pętla ----------

    private void Update()
    {
        if (_timer.IsRunning != _lastRunning) RefreshControls();

        if (_timer.IsRunning)
        {
            if (_smoothProgress && _progressFill)
                _progressFill.fillAmount = 1f - Mathf.Clamp01(_timer.RemainingTime / _phaseDuration);
            return;
        }

        if (TodoItemUI.AnyEditing) return;
        
        // INACTIVE - strzałki działają cały czas
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

        if (Input.GetKeyDown(_confirmKey)) PlayTimer();
    }

    private void BeginHold(int dir)
    {
        _holdDir = dir;
        _holdTimer = _repeatDelay;
        Step(dir);
    }

    // ---------- reakcja na timer ----------

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