using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.UI;

public class PomodoroSessionUI : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private timer_service _timer;
    [SerializeField] private DeskWorkstation _desk;

    [Header("Wyświetlanie")] [SerializeField]
    private TMP_Text _timeText;

    [SerializeField] private TMP_Text _stateText;
    [SerializeField] private Image _background;
    [SerializeField] private Image _progressFill;
    [SerializeField] private string _workLabel = "SKUPIENIE";
    [SerializeField] private string _breakLabel = "PRZERWA";

    [Header("Kolory faz")] [SerializeField]
    private Color _workColor;
    [SerializeField] private Color _breakColor;

    [Header("Przyciski")] [SerializeField] private Button _pauseButton;
    [SerializeField] private Button _skipButton;
    [SerializeField] private Button _stopButton;
    [SerializeField] private TMP_Text _pauseLabel;

    [Header("Pasek postępu")] [SerializeField]
    private bool _smoothProgress = true;

    private float _phaseDuration = 1f;

    private void Awake()
    {
        if (_pauseButton) _pauseButton.onClick.AddListener(TogglePause);
        if(_skipButton) _skipButton.onClick.AddListener(()=> _timer.SkipPhase());
        if(_stopButton) _stopButton.onClick.AddListener(()=> _desk.StandUp());

        if (_timer)
        {
            _timer.OnPhaseChange += HandlePhaseChange;
            _timer.OnTick += HandleTick;
            
        }
        if(_panel) _panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_timer)
        {
            _timer.OnPhaseChange -= HandlePhaseChange;
            _timer.OnTick -= HandleTick;
        }
    }

    public void Show()
    {
        if(_panel) _panel.SetActive(true);
        _phaseDuration = Mathf.Max(0.01f,_timer.PhaseDuration);
        HandlePhaseChange(_timer.phase);
        HandleTick(_timer.RemainingTime);
    }

    public void Hide()
    {
        if(_panel) _panel.SetActive(false);
    }

    private bool Visible => _panel && _panel.activeSelf;
    public void Update()
    {
        if(!_smoothProgress || !Visible || !_progressFill) return;
        _progressFill.fillAmount = 1f - Mathf.Clamp01(_timer.RemainingTime / _phaseDuration);
    }

    private void HandlePhaseChange(PomodoroPhase phase)
    {
        if(!Visible) return;
        if(phase == PomodoroPhase.Idle) return;
        
        bool work = phase == PomodoroPhase.Work;
        Color c = work ? _workColor : _breakColor;
        if(_stateText) _stateText.text = work? _workLabel : _breakLabel;
        if(_background) _background.color = c;
        if(_progressFill) _progressFill.color = c;
        
        _phaseDuration = Mathf.Max(0.01f, _timer.PhaseDuration);
        UpdatePauseLabel();
        
    }

    private void HandleTick(float remaining)
    {
        if(!Visible) return;
        
        if(_timeText) _timeText.text = _timer.TimeString;
        
        if(!_smoothProgress && _progressFill)
            _progressFill.fillAmount = 1f - Mathf.Clamp01(remaining / _phaseDuration);
    }

    private void TogglePause()
    {
        if(_timer.IsRunning) _timer.Pause();
        else _timer.Resume();
        UpdatePauseLabel();
    }

    private void UpdatePauseLabel()
    {
        if (_pauseLabel)
            _pauseLabel.text = _timer.IsRunning ? "PAUZA" : "WZNÓW";
    }
}
