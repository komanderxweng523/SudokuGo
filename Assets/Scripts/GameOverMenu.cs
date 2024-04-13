using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameOverMenu : MonoBehaviour
{
    public Text textTimer;
    // Start is called before the first frame update
    void Start()
    {
        textTimer.text = Timer.instance.GetCurrentTimeText().text;
    }
}
