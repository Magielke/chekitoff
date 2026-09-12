using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FocusRewardPopup : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    [SerializeField] private timer_service _timer;
    [SerializeField] private CurrencyService _currency;
    [SerializeField] private PanelReveal _panel;

    [Header("Teksty")]
    [SerializeField] private TMP_Text _amountText;
    [SerializeField] private TMP_Text _headlineText;
    [SerializeField] private TMP_Text _detailText;
    [SerializeField] private string _completedHeadline = "SESJA UKOŃCZONA";
    [SerializeField] private string _abortedHeadline = "SESJA PRZERWANA";

    [Header("Przelicznik")]
    [SerializeField] private float _completedRate = 1.0f;
    [SerializeField] private float _abortedRate = 0.85f;
    [SerializeField] private int _minMinutesToReward = 1;
    [SerializeField] private bool _minimumOnePoint = true;
    [Tooltip("Tolerancja przy uznaniu sesji za ukończoną (sekundy).")]
    [SerializeField] private float _completionTolerance = 1.5f;

    [Header("Przyciski")]
    [SerializeField] private Button _closeButton;

    [Header("Blokada gry")]
    [SerializeField] private Behaviour[] _disableWhileOpen;

    private PomodoroPhase _lastPhase = PomodoroPhase.Idle;
    private float _workElapsed;
    private float _workPlanned;
    private bool _trackingWork;
    private bool _suppressNext;

    private void Awake()
    {
        if (_closeButton) _closeButton.onClick.AddListener(Close);
    }

    private void Start()
    {
        IsOpen = false;
        if (_panel) _panel.Hide();
    }

    private void OnEnable()
    {
        if (_timer) _timer.OnPhaseChange += HandlePhaseChange;
    }

    private void OnDisable()
    {
        if (_timer) _timer.OnPhaseChange -= HandlePhaseChange;
        IsOpen = false;
    }

    /// <summary>Wywołaj przed Stop(), gdy nagroda ma nie zostać przyznana.</summary>
    public void SuppressNext() => _suppressNext = true;

    private void Update()
    {
        if (_timer && _timer.IsRunning && _timer.phase == PomodoroPhase.Work)
            _workElapsed += Time.deltaTime;
    }

    private void HandlePhaseChange(PomodoroPhase phase)
    {
        if (_trackingWork && phase != PomodoroPhase.Work)
        {
            _trackingWork = false;

            if (_suppressNext) { _suppressNext = false; _workElapsed = 0f; }
            else
            {
                bool completed = _workElapsed >= _workPlanned - _completionTolerance;
                GrantReward(completed);
            }
        }

        if (phase == PomodoroPhase.Work)
        {
            _workPlanned = _timer.PhaseDuration;
            if (_lastPhase != PomodoroPhase.Work) _workElapsed = 0f;
            _trackingWork = true;
        }

        _lastPhase = phase;
    }

    private void GrantReward(bool completed)
    {
        int minutes = completed
            ? Mathf.RoundToInt(_workPlanned / 60f)
            : Mathf.FloorToInt(_workElapsed / 60f);

        _workElapsed = 0f;

        if (minutes < _minMinutesToReward) return;

        float rate = completed ? _completedRate : _abortedRate;
        int amount = Mathf.FloorToInt(minutes * rate);
        if (_minimumOnePoint) amount = Mathf.Max(1, amount);

        if (amount <= 0) return;

        if (_currency) _currency.Add(amount);
        ShowPopup(amount, minutes, completed);
    }

    private void ShowPopup(int amount, int minutes, bool completed)
    {
        if (_headlineText) _headlineText.text = completed ? _completedHeadline : _abortedHeadline;
        if (_amountText)   _amountText.text = $"+{amount}";

        if (_detailText)
        {
            _detailText.text = completed
                ? $"{minutes} min skupienia"
                : $"{minutes} min skupienia x {_abortedRate:0.##}";
        }

        IsOpen = true;
        SetGameInput(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (_panel) _panel.Show();
    }

    public void Close()
    {
        IsOpen = false;
        SetGameInput(true);

        if (_panel) _panel.Hide();
    }

    private void SetGameInput(bool enabled)
    {
        if (_disableWhileOpen == null) return;
        foreach (var b in _disableWhileOpen)
            if (b) b.enabled = enabled;
    }
}