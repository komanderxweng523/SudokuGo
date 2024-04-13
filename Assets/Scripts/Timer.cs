using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class Timer : MonoBehaviour
{
    private Text textTimer;
    private float delta_time;
    private bool stop_clock_ = false;

    public static Timer instance;

    private void Awake()
    {
        if(instance)
            Destroy(instance);

        instance = this;

        textTimer = GetComponent<Text>();
        delta_time = 0;
    }

    // Start is called before the first frame update
    void Start()
    {
        stop_clock_ = false;
    }

    // Update is called once per frame
    void Update()
    {
        if(stop_clock_ == false)
        {
            delta_time += Time.deltaTime;
            TimeSpan span = TimeSpan.FromSeconds(delta_time);

            string minute = LoadingZero(span.Minutes);
            string seconds = LoadingZero(span.Seconds);

            textTimer.text = minute + ":" + seconds;
        }
    }

    string LoadingZero(int n)
    {
        return n.ToString().PadLeft(2, '0');
    }

    public void OnGameOver()
    {
        stop_clock_ = true;
    }

    public void OnBoardComplete()
    {
        stop_clock_ = true;
    }

    public void OnCancelSearch()
    {
        stop_clock_ = true;
        delta_time = 0;
    }

    private void OnEnable()
    {
        GameEvents.OnGameOver += OnGameOver;
        GameEvents.OnBoardComplete += OnBoardComplete;
        GameEvents.OnCancelSearch += OnCancelSearch;
    }

    private void OnDisable()
    {
        GameEvents.OnGameOver -= OnGameOver;
        GameEvents.OnBoardComplete -= OnBoardComplete;
        GameEvents.OnCancelSearch -= OnCancelSearch;
    }

    public Text GetCurrentTimeText()
    {
        return textTimer;
    }
}
