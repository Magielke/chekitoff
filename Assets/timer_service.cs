using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;

public enum PomodoroPhase
{
    Idle,
    Work,
    Break
}

public class timer_service : MonoBehaviour
{

    [Header("Domyślne wartości pomodoro")] [SerializeField]
    private float _DefaultWorkMinutes = 25f;

    [SerializeField] private float _DefaultBreakMinutes = 5f;
    [SerializeField] private bool _loopPhases = true;

    [Header("Wyświetlanie (Ekran na biurku)")] [SerializeField]
    private TMP_Text _timeText;

    [SerializeField] private TMP_Text _stateText;
    [SerializeField] private string _workLabel = "PRACA";
    [SerializeField] private string _breakLabel = "PRZERWA";
    [SerializeField] private string _idleLabel = "-";


    [Header("Tło")] [SerializeField] private Renderer _backgroundRenderer;
    [SerializeField] private string _colorProperty = "_BaseColor";
    [SerializeField] private Graphic _backgroundGraphic;
    [SerializeField] private Color _workColor;
    [SerializeField] private Color _breakColor;
    [SerializeField] private Color _idleColor;
    [SerializeField] private bool _tintText = false;
    
    [Header("Animacja gracza (poza siadaniem")] [SerializeField]
    private Animator _playerAnimator;

    [SerializeField] private string _breakBoolParam = "Focus";

    //Zdarzenia zewnętrzne
    public event Action<PomodoroPhase> OnPhaseChange;
    public event Action<float> OnTick;
    public event Action OnFinished;

    //Stan
    private PomodoroPhase _phase = PomodoroPhase.Idle;
    private float _workDuration, _breakDuration;
    private float _time, _timerTemp, _remainingTime;
    private bool _running;

    public PomodoroPhase phase => _phase;
    public float RemainingTime => _remainingTime;
    public float PhaseDuration => _phase == PomodoroPhase.Work ? _workDuration : _timerTemp;
    public bool IsRunning => _running;
    public string TimeString => Format(_remainingTime);

    public string StateString => _phase == PomodoroPhase.Work ? _workLabel
        : _phase == PomodoroPhase.Break ? _breakLabel
        : _idleLabel;

    private void Awake()
    {
        _workDuration = _DefaultWorkMinutes * 60f;
        _breakDuration = _DefaultBreakMinutes * 60f;
        ApplyVisuals();
        RefreshDisplay();
    }

    //API
    public void SetLoop(bool loop) => _loopPhases = loop;
    
    public void Configure(float workMinutes, float breakMinutes)
    {
        _workDuration = Mathf.Max(1f, workMinutes * 60f);
        _breakDuration = Mathf.Max(1f, breakMinutes * 60f);
    }

    public void ConfigureSeconds(float workSeconds, float breakSeconds)
    {
        _workDuration = Mathf.Max(1f, workSeconds);
        _breakDuration = Mathf.Max(1f, breakSeconds);
    }

    public void StartTimer() => EnterPhase(PomodoroPhase.Work);
    public void StartWork() => EnterPhase(PomodoroPhase.Work);
    public void StartBreak() => EnterPhase(PomodoroPhase.Break);

    public void StartTimer(float workMinutes, float breakMinutes)
    {
        Configure(workMinutes, breakMinutes);
        EnterPhase(PomodoroPhase.Work);
    }

    public void Pause() => _running = false;

    public void Resume()
    {
        if (_phase != PomodoroPhase.Idle)
        {
            _running = true;
        }
    }

    public void SkipPhase()
    {
        if(_phase == PomodoroPhase.Idle)
            HandlePhaseEnd();
    }

    public void Stop()
    {
        _running = false;
        _phase = PomodoroPhase.Idle;
        _remainingTime = 0f;
        _time = 0f;
        _timerTemp = 0f;
        SetAnimatorBreak(false);
        ApplyVisuals();
        RefreshDisplay();
        OnPhaseChange?.Invoke(_phase);
    }

    //Pętla
    private void Update()
    {
        if (!_running) return;

        _timerTemp = _time;
        _time += Time.deltaTime;
        _remainingTime -= Time.deltaTime;

        if (_remainingTime <= 0f)
        {
            _remainingTime = 0;
            RefreshDisplay();
            OnTick?.Invoke(0f);
            HandlePhaseEnd();
            return;
        }

        if ((int)_time != (int)_timerTemp)
        {
            RefreshDisplay();
            OnTick?.Invoke(_remainingTime);
        }
    }

    private void HandlePhaseEnd()
    {
        if (_phase == PomodoroPhase.Work)
        {
            EnterPhase(PomodoroPhase.Break);
        }
        else if (_phase == PomodoroPhase.Break)
        {
            if(_loopPhases)
                EnterPhase(PomodoroPhase.Work);
            else
            {
                Stop();
                OnFinished?.Invoke();
            }
        }
    }

    private void EnterPhase(PomodoroPhase phase)
    {
        _phase = phase;
        _remainingTime = _phase == PomodoroPhase.Work ? _workDuration : _breakDuration;
        _time = 0f;
        _timerTemp = 0f;
        _running = true;
        
        SetAnimatorBreak(phase == PomodoroPhase.Break);
        ApplyVisuals();
        RefreshDisplay();
        OnPhaseChange?.Invoke(_phase);

    }

    //prezentacja
    private void RefreshDisplay()
    {
        if (_timeText) _timeText.text = TimeString;
        if(_stateText) _stateText.text = StateString;
    }

    private void ApplyVisuals()
    {
        Color color = _phase == PomodoroPhase.Work ? _workColor
            : _phase == PomodoroPhase.Break ? _breakColor
            : _idleColor;

        if (_backgroundRenderer)
        {
            var mat = _backgroundRenderer.material;
            if (mat.HasProperty(_colorProperty)) mat.SetColor(_colorProperty, color);
            else mat.color = color;
        }

        if (_backgroundGraphic)
        {
            _backgroundGraphic.color = color;
        }

        if (_tintText)
        {
            if(_timeText) _timeText.color = color;
            if(_stateText) _stateText.color = color;
        }
        
    }

    private static string Format(float seconds)
    {
        TimeSpan t = TimeSpan.FromSeconds(Mathf.Max(0f, seconds));
        return t.Hours>0? $"{t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}" : $"{t.Minutes:D2}:{t.Seconds:D2}";
        
    }

    private void SetAnimatorBreak(bool onBreak)
    {
        if (_playerAnimator || string.IsNullOrEmpty(_breakBoolParam)) return;
        _playerAnimator.SetBool(_breakBoolParam, onBreak);
    }
}