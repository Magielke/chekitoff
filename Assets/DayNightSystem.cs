using System;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class DayNightSystem : MonoBehaviour
{
    [Header("Time system")]
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text dateText;
    [SerializeField] private string format = "ddd dd/MM";
    

    // public GameObject sunLight;
    [Header("Lightning system")]
    public GameObject NightLightSet;
    public List<LightData> NightLights = new List<LightData>();
    public GameObject DayLightSet;
    public List<LightData> DayLights = new List<LightData>();
    public float timeOfLightChange = 2f;
    
    [Header("DEBUG")]
    public bool DEBUG_OVERRIDE_STATE_CHANGE = false;

    
    [Header("Check interval")]
    [SerializeField] private float checkIntervalSeconds = 60f;

    [Header("Current state")]
    [SerializeField] private int currentHour;
    [SerializeField] private int currentMonth;
    [SerializeField] private DayState currentState;

    public enum DayState { Night, Day }

    [Header("Events")]
    public UnityEvent<DayState> OnStateChanged;
    public UnityEvent<int> OnHourChanged;
    
    [Header("Skybox")]
    [SerializeField] private Material _skyboxMaterial;
    [SerializeField] private float _blendSpeed = 0.5f;
    [SerializeField] private bool _instantOnStart = true;

    private bool _isLightStable = false;
    private bool _debug_change = false;
    
    private DayState _lastState;
    private int _lastHour;
    private float _timer;
    private DateTime showDate;
    
    private static readonly int BlendID = Shader.PropertyToID("_Blend");

    private float _targetBlend;
    private float _currentBlend;

    private void Awake()
    {
        if(DayLightSet && !DayLightSet.activeSelf) DayLightSet.SetActive(true);
        if(NightLightSet && !NightLightSet.activeSelf) NightLightSet.SetActive(true);
        if (_skyboxMaterial)
        {
            _skyboxMaterial = new Material(_skyboxMaterial);
            RenderSettings.skybox = _skyboxMaterial;
        }
    }

    void Start()
    {
        ForceInitialState();
        
        DayLights = CollectLights(DayLightSet);
        NightLights = CollectLights(NightLightSet);

        bool night = WantNight();
        foreach(LightData d in DayLights) d.light.intensity = night ? 0f:d.intensity;
        foreach(LightData n in NightLights) n.light.intensity = night ? n.intensity : 0f;

        _isLightStable = true;
        _debug_change = DEBUG_OVERRIDE_STATE_CHANGE;
    }
    
    private void ForceInitialState()
    {
        DateTime  now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.Local);
        currentHour = now.Hour;
        currentMonth = now.Month;

        _lastHour = now.Hour;
        _lastState = GetState(currentHour, currentMonth);
        currentState = _lastState;
        
        OnStateChanged?.Invoke(_lastState);
        OnHourChanged?.Invoke(_lastHour);
        OnHourChange(currentHour);
        OnDayNightChange(currentState);
        
        ApplySkybox(WantNight(),_instantOnStart);
    }

    private bool WantNight()
    {
        bool night = currentState == DayState.Night;
        if(DEBUG_OVERRIDE_STATE_CHANGE) night = !night;
        return night;
    }
    
    private void ApplySkybox(bool isNight, bool instant)
    {
        _targetBlend = isNight ? 1f : 0f;
        if (instant)
        {
            _currentBlend = _targetBlend;
            if (_skyboxMaterial)
            {
                _skyboxMaterial.SetFloat(BlendID, _currentBlend);
                DynamicGI.UpdateEnvironment();
            }
        }
    }
    
    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= checkIntervalSeconds)
        {
            _timer = 0;
            Check();
        }
        
        DateTime now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.Local);
        if(timeText) timeText.text = now.ToString("HH:mm");

        if (DEBUG_OVERRIDE_STATE_CHANGE != _debug_change)
        {
            _debug_change = DEBUG_OVERRIDE_STATE_CHANGE;
            _isLightStable = false;
        }
        
        bool wantNight = WantNight();
        ApplySkybox(wantNight,false);

        UpdateLights(wantNight);
        UpdateSkyboxBlend();
    }

    private void UpdateLights(bool wantNight)
    {
        if (_isLightStable) return;
        
        float k= 1f - Mathf.Pow(0.01f, Time.deltaTime/Mathf.Max(0.01f, timeOfLightChange));
        bool allDone = true;

        foreach (LightData d in DayLights)
        {
            if (!d || !d.light) continue;
            float target = wantNight ? 0f : d.intensity;
            d.light.intensity = Mathf.Lerp(d.light.intensity, target, k);
            
            if(Mathf.Abs(d.light.intensity - target) > 0.01f) allDone = false;
            else d.light.intensity = target;
        }
        foreach (LightData n in NightLights)
        {
            if (!n || !n.light) continue;
            float target = wantNight ?  n.intensity:0f;
            n.light.intensity = Mathf.Lerp(n.light.intensity, target, k);
            
            if(Mathf.Abs(n.light.intensity - target) > 0.01f) allDone = false;
            else n.light.intensity = target;
        }
        _isLightStable = allDone;
    }

    private void UpdateSkyboxBlend()
    {
        if(!_skyboxMaterial) return;
        if(Mathf.Approximately(_currentBlend,_targetBlend)) return;
        
        _currentBlend = Mathf.MoveTowards(_currentBlend,_targetBlend,_blendSpeed * Time.deltaTime);
        _skyboxMaterial.SetFloat(BlendID, _currentBlend);
        DynamicGI.UpdateEnvironment();
    }

    private List<LightData> CollectLights(GameObject root)
    {
        var result = new List<LightData>();
        if (root == null)
        {
            return result;
        }
        foreach (Light light in root.GetComponentsInChildren<Light>(true))
        {
            LightData lightData = light.GetComponent<LightData>();
            if (lightData == null)
            {
                lightData = light.gameObject.AddComponent<LightData>();
            }

            lightData.light = light;
            lightData.intensity = light.intensity;
            result.Add(lightData);
        }
        return result;
    }

    void Check()
    {
        DateTime now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.Local);
        currentHour = now.Hour;
        currentMonth = now.Month;

        if (currentHour != _lastHour)
        {
            _lastHour = currentHour;
            OnHourChanged?.Invoke(currentHour);
            OnHourChange(currentHour);
        }

        DayState newState = GetState(currentHour, currentMonth);
        if (newState != _lastState)
        {
            _lastState = newState;
            currentState = newState;
            OnStateChanged?.Invoke(currentState);
            OnDayNightChange(currentState);
        }
    }

    // Override these in a subclass, or just add your logic directly here
    protected virtual void OnDayNightChange(DayState newState)
    {
        _isLightStable = false;
        switch (newState)
        {
            case DayState.Night: HandleNight(); break;
            case DayState.Day: HandleDay(); break;
        }
    }

    protected virtual void OnHourChange(int hour) { }

    protected virtual void HandleNight() { Debug.Log("Night"); }
    protected virtual void HandleDay() { Debug.Log("Day"); }

    DayState GetState(int hour, int month)
    {
        GetSunTimes(month, out int sunrise, out int sunset);

        return(hour >= sunrise && hour <= sunset) ? DayState.Day : DayState.Night;
    }

    void GetSunTimes(int month, out int sunrise, out int sunset)
    {
        switch (month)
        {
            case 12: case 1: sunrise = 8; sunset = 16; break;
            case 2: case 11: sunrise = 7; sunset = 17; break;
            case 3: case 10: sunrise = 6; sunset = 18; break;
            case 4: case 9: sunrise = 5; sunset = 19; break;
            case 5: case 8: sunrise = 5; sunset = 20; break;
            case 6: case 7: sunrise = 4; sunset = 21; break;
            default: sunrise = 6; sunset = 18; break;
        }
    }

    public DayState GetCurrentState() => currentState;
    public bool IsNight() => currentState == DayState.Night;
    public bool IsDaytime() => currentState == DayState.Day;
    public float GetDayProgress()
    {
        GetSunTimes(currentMonth, out int sunrise, out int sunset);
        return Mathf.Clamp01((float)(currentHour - sunrise) / (sunset - sunrise));
    }

    private void OnEnable()
    {
        Refresh();
        StartCoroutine(WatchForDateChange());
    }

    private IEnumerator WatchForDateChange()
    {
        var wait = new WaitForSeconds(1f);
        while (true)
        {
            yield return wait;
            if (DateTime.Now.Date != showDate) Refresh();
        }
    }

    private void Refresh()
    {
        showDate = DateTime.Now.Date;
        dateText.text = showDate.ToString(format, CultureInfo.InvariantCulture);
    }
}
