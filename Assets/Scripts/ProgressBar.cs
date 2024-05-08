using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode()]
public class ProgressBar : MonoBehaviour
{
    public int maximum;
    public int current;
    public Image mask;
    public Text currentNumber;

    void Start()
    {
        current = 0;
        currentNumber.text = current.ToString();
    }

    void Update()
    {
        maximum = SharedData.GeneratedNumber;
    }

    private void CorrectNumber()
    {
        if(current < maximum)
        {
            current += 1;
            float fillAmount = (float)current / (float)maximum;
            mask.fillAmount = fillAmount;
            currentNumber.text = current.ToString(); 
        }
    }

    private void OnEnable()
    {
        GameEvents.OnCorrectNumber += CorrectNumber;
    }

    private void OnDisable()
    {
        GameEvents.OnCorrectNumber -= CorrectNumber;
    }
}
