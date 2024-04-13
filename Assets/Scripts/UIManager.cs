using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    //Screen object variables
    public GameObject loginUI;
    public GameObject registerUI;
    public GameObject forgotPasswordUI;
    public GameObject loadingUI;
    public GameObject welcomeUI;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != null)
        {
            Debug.Log("Instance already exists, destroying object!");
            Destroy(this);
        }
    }

    //Functions to change the login screen UI
    public void LoginScreen() //Back button
    {
        loginUI.SetActive(true);
        registerUI.SetActive(false);
        forgotPasswordUI.SetActive(false);
        welcomeUI.SetActive(false);
    }
    public void RegisterScreen() // Regester button
    {
        loginUI.SetActive(false);
        registerUI.SetActive(true);
    }

    public void ForgotPasswordScreen()
    {
        loginUI.SetActive(false);
        forgotPasswordUI.SetActive(true);
    }

    public void LoadingScreen()
    {
        loadingUI.SetActive(true);
        Task.Delay(3000);
    }

    public void CloseLoadingScreen()
    {
        loadingUI.SetActive(false);
    }

    public void WelcomeScreen()
    {
        loginUI.SetActive(false);
        welcomeUI.SetActive(true);
    }

    public void EnterGame()
    {
        SceneManager.LoadSceneAsync("MainMenu");
    }
}