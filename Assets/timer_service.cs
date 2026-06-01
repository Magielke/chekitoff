using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class timer_service : MonoBehaviour
{
    private List<TimerHistory> timerHistory = new List<TimerHistory>();
    private float startTime;
    private bool timerStarted = false;
    private bool timerPaused = false;

    public float Minutes = 10;
    public float Seconds = 0;
    public TextMeshProUGUI Text;

    private float _time;
    private float _remainingTime;
    private float _timerTemp;
    private bool statechanged = false;

    private bool displayRemainingTime = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _time = Minutes * 60 + Seconds;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            displayRemainingTime = !displayRemainingTime;
            statechanged = true;
        }
        else if (Input.GetKeyDown(KeyCode.F) && !timerStarted)
        {
            //Start  timer
            startTime = Time.time;
            _time = 0;
            _remainingTime = Minutes * 60 + Seconds;
            timerStarted = true;
            statechanged = true;
        }

        else if (Input.GetKeyDown(KeyCode.F) && timerStarted)
        {
            if (!timerPaused)
            {
                //Pause timer
                timerPaused = true;
                statechanged = true;
            }
            else
            {
                //Resume timer
                timerPaused = false;
                statechanged = true;
            }
        }
        else if (Input.GetKeyDown(KeyCode.X) && timerStarted)
        {
            //Stop timer
            timerStarted = false;
            TimerHistory history = new TimerHistory();
            history.startTime = startTime;
            history.endTime = Time.time;
            history.completed = false;
            history.totalTime = Minutes * 60 + Seconds;
            history.elapsedTime = _time;
            timerHistory.Add(history);
            statechanged = true;
            
        }

        if ((timerStarted && !timerPaused ))
        {
            _timerTemp = _time;
            _time += Time.deltaTime;
            _remainingTime -= Time.deltaTime;
            if (_remainingTime < 0 || _time > Minutes * 60 + Seconds)
            {
                timerStarted = false;
                TimerHistory history = new TimerHistory();
                history.startTime = startTime;
                history.endTime = Time.time;
                history.completed = true;
                history.totalTime = Minutes * 60 + Seconds;
                history.elapsedTime = _time;
                timerHistory.Add(history);
            }
            //check if timertemp and time variables have the same seconds or not
            
        }
        if ((int)_time != (int)_timerTemp || _time == 0 || statechanged)
        {
            TimeSpan t;
            if (displayRemainingTime)
            {
                t = TimeSpan.FromSeconds(_remainingTime);

            }
            else
            {
                t = TimeSpan.FromSeconds(_time);
            }
            Text.text = t.Hours > 0
                ? string.Format("{0:D2}:{1:D2}:{2:D2}", t.Hours, t.Minutes, t.Seconds)
                : string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);

            if (statechanged && timerPaused)
            {
                Text.color = Color.yellow;
            }

            if (statechanged && !timerPaused)
            {
                Text.color = Color.white;
            }

            if (!timerStarted)
            {
                Text.gameObject.SetActive(false);
            }

            if (timerStarted && !Text.gameObject.activeInHierarchy)
            {
                Text.gameObject.SetActive(true);
            }
        }
 
    }
}

struct TimerHistory
{

       public float startTime;
       public float endTime;
       public bool completed;
       public float totalTime;
       public float elapsedTime;

}
