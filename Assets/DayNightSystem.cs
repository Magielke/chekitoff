using System;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
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
    private bool _islightStable = false;
    public bool DEBUG_OVERRIDE_STATE_CHANGE = false;
    private bool _debug_change = false;
    public float timeOfLightChange;
    [Header("Check interval")]
    [SerializeField] private float checkIntervalSeconds = 60f;

    [Header("Current state")]
    [SerializeField] private int currentHour;
    [SerializeField] private int currentMonth;
    [SerializeField] private DayState currentState;

    public enum DayState { Night, Dawn, Morning, Afternoon, Evening, Dusk }

    [Header("Events")]
    public UnityEvent<DayState> OnStateChanged;
    public UnityEvent<int> OnHourChanged;

    private DayState _lastState;
    private int _lastHour;
    private float _timer;
    private DateTime showDate;

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
    void Start()
    {
        Check();
        DayLights = CollectLights(DayLightSet);
        NightLights = CollectLights(NightLightSet);
     
        var toDim = (currentState == DayState.Night) ?        DayLights : NightLights;
        foreach (LightData light in toDim)
        {
            light.light.intensity = 0f;
        }
    }
    

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= checkIntervalSeconds)
        {
            _timer = 0f;
            Check();
        }
        DateTime now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.Local);
        float hour = now.Hour + now.Minute / 60f;

        // 0h = -90 (below horizon), 12h = 90 (zenith), 24h = -90
        float angle = (hour / 24f) * 360f - 90f;
        // if (sunLight != null)
        //     sunLight.transform.rotation = Quaternion.Euler(angle, 170f, 0f);
        // else
        // {
        //     Debug.LogError("DayNightSystem: sunLight isn't setted up");
        // }
        timeText.text = now.ToString("HH:mm");
        if (DEBUG_OVERRIDE_STATE_CHANGE != _debug_change)
        {
            _islightStable = false;
            _debug_change = DEBUG_OVERRIDE_STATE_CHANGE;
        }
        
        if (_islightStable  == false)
        {
            if (currentState == DayState.Night || (DEBUG_OVERRIDE_STATE_CHANGE && currentState != DayState.Night))
            {
                foreach (LightData daylight in DayLights)
                {
                    daylight.light.intensity = Mathf.Lerp(
                        daylight.light.intensity,
                        0f,
                        1f - Mathf.Pow(0.01f, Time.deltaTime / timeOfLightChange)
                    );

                    if (daylight.light.intensity <= 0.001f)
                    {
                        daylight.light.intensity = 0f;
                        _islightStable = true;
                    }
                }
                foreach (LightData nightlight in NightLights)
                {
                    nightlight.light.intensity = Mathf.Lerp(
                        nightlight.light.intensity,
                        nightlight.intensity,
                        1f - Mathf.Pow(0.01f, Time.deltaTime / timeOfLightChange)
                    );
                }
            }
            else
            {
                foreach (LightData daylight in DayLights)
                {
                    daylight.light.intensity = Mathf.Lerp(
                        daylight.light.intensity,
                        daylight.intensity,
                        1f - Mathf.Pow(0.01f, Time.deltaTime / timeOfLightChange)
                    );
                }
                foreach (LightData nightlight in NightLights)
                {
                    nightlight.light.intensity = Mathf.Lerp(
                        nightlight.light.intensity,
                        0f,
                        1f - Mathf.Pow(0.01f, Time.deltaTime / timeOfLightChange)
                    );

                    if (nightlight.light.intensity <= 0.001f)
                    {
                        nightlight.light.intensity = 0f;
                        _islightStable = true;
                    }
                }
            }
        }
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
        switch (newState)
        {
            case DayState.Night: HandleNight(); break;
            case DayState.Dawn: HandleDawn(); break;
            case DayState.Morning: HandleMorning(); break;
            case DayState.Afternoon: HandleAfternoon(); break;
            case DayState.Evening: HandleEvening(); break;
            case DayState.Dusk: HandleDusk(); break;
        }
    }

    protected virtual void OnHourChange(int hour) { }

    protected virtual void HandleNight() { Debug.Log("Night"); _islightStable = false; }
    protected virtual void HandleDawn() { Debug.Log("Dawn"); }
    protected virtual void HandleMorning() { Debug.Log("Morning");
        _islightStable = false;
    }
    protected virtual void HandleAfternoon() { Debug.Log("Afternoon"); }
    protected virtual void HandleEvening() { Debug.Log("Evening"); }
    protected virtual void HandleDusk() { Debug.Log("Dusk"); }

    DayState GetState(int hour, int month)
    {
        GetSunTimes(month, out int sunrise, out int sunset);

        if (hour < sunrise - 1 || hour >= sunset + 2) return DayState.Night;
        if (hour == sunrise - 1) return DayState.Dawn;
        if (hour >= sunrise && hour < sunrise + 4) return DayState.Morning;
        if (hour >= sunrise + 4 && hour < sunset - 2) return DayState.Afternoon;
        if (hour == sunset - 1 || hour == sunset - 2) return DayState.Evening;
        if (hour == sunset || hour == sunset + 1) return DayState.Dusk;

        return DayState.Night;
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
    public bool IsDaytime() => currentState == DayState.Morning || currentState == DayState.Afternoon;
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
