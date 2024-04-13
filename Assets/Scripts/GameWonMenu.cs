using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameWonMenu: MonoBehaviour
{
    public GameObject GameWinUI;
    public Text TimerText;
    // Start is called before the first frame update
    void Start()
    {
        GameWinUI.SetActive(false);
        TimerText.text = Timer.instance.GetCurrentTimeText().text;
    }

    private void OnBoardComplete()
    {
        GameWinUI.SetActive(true);
        TimerText.text = Timer.instance.GetCurrentTimeText().text;
    }
    
    private void OnEnable()
    {
        GameEvents.OnBoardComplete += OnBoardComplete;
    }

    private void OnDisable()
    {
        GameEvents.OnBoardComplete -= OnBoardComplete;
    }
}
