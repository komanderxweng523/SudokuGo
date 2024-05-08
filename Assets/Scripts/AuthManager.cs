using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using System.Threading.Tasks;
using System.Globalization;

public class AuthManager : MonoBehaviour
{
    public InputField usernameLogin;
    public InputField passwordLogin;
    public Text loginText;

    public InputField usernameRegister;
    public InputField passwordRegister;
    public InputField confirmPassword;
    public Text registerText;

    public InputField forgotPassword;
    public Text forgotPasswordText;
    
    public Text welcomeText;

    private async void Start()
    {
       await UnityServices.InitializeAsync();
       Debug.Log(UnityServices.State);
    }

    public async void LoginButton()
    {
        UIManager.instance.LoadingScreen();
        await SignInWithUsernamePassword(usernameLogin.text, passwordLogin.text);
    }

    public async void RegisterButton()
    {
        UIManager.instance.LoadingScreen();
        await SignUpWithUsernamePassword(usernameRegister.text, passwordRegister.text);
    }

    /*public void ForgotPasswordButton()
    {
        UIManager.instance.LoadingScreen();
        StartCoroutine(ForgotPassword(forgotPassword.text));
    }*/

    public async void SignOutButton()
    {
        UIManager.instance.LoadingScreen();
        await SignOutOfGame(); 
    }

    async Task SignOutOfGame()
    {
        try
        {
            AuthenticationService.Instance.SignOut(true);
            Debug.LogFormat("User logged out successfully");
            UIManager.instance.CloseLoadingScreen();
            UIManager.instance.LoginScreen();
            clearAllText();
        }
        catch(AuthenticationException ex)
        {
            Debug.LogException(ex);
            UIManager.instance.CloseLoadingScreen();
        }
        catch(RequestFailedException ex)
        {
            Debug.LogException(ex);
            UIManager.instance.CloseLoadingScreen();
        }
    }
     
    public void clearAllText()
    {
        usernameLogin.text = "";
        passwordLogin.text = "";
        usernameRegister.text = "";
        passwordRegister.text = "";
        confirmPassword.text = "";
        loginText.text = "";
        welcomeText.text = "";
    }

    async Task SignInWithUsernamePassword(string username, string password)
    {
        try
        {
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
            UIManager.instance.CloseLoadingScreen();
            Debug.LogFormat("User signed in successfully: " + username + " with password " + password);
            UIManager.instance.WelcomeScreen();
            welcomeText.text = "Welcome back, " + username;
        }
        catch(AuthenticationException ex)
        {
            Debug.LogException(ex);
            UIManager.instance.CloseLoadingScreen();
            Debug.LogError(password);
        }
        catch (RequestFailedException ex)
        {
            Debug.LogException(ex);
            UIManager.instance.CloseLoadingScreen();
        }
    }

    /*private IEnumerator ForgotPassword(string _email)
    {
        if(_email == "")
        {
            forgotPasswordText.text = "Missing Email";
        }
        else
        {
            Task ForgotPasswordTask = auth.SendPasswordResetEmailAsync(_email);
            yield return new WaitUntil(predicate: () => ForgotPasswordTask.IsCompleted);

            if(ForgotPasswordTask.Exception != null)
            {
                Debug.LogWarning(message: $"Failed to continue task with {ForgotPasswordTask.Exception}");
                FirebaseException firebaseEx = ForgotPasswordTask.Exception.GetBaseException() as FirebaseException;
                AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

                string message = "Submit Failed!";
                switch (errorCode)
            {
                case AuthError.InvalidEmail:
                    message = "Invalid Email";
                    break;
                case AuthError.UserNotFound:
                    message = "Account does not exist";
                    break;
            }
            forgotPasswordText.text = message;
            UIManager.instance.CloseLoadingScreen();
            }
        }
    }*/

    async Task SignUpWithUsernamePassword(string username, string password)
    {
        if (username == "")
        {
            //If the username field is blank show a warning
            registerText.text = "Missing Username";
            UIManager.instance.CloseLoadingScreen();
        }
        else if(passwordRegister.text != confirmPassword.text)
        {
            //If the password does not match show a warning
            registerText.text = "Password Does Not Match!";
            UIManager.instance.CloseLoadingScreen();
        }
        else
        {
            try
            {
                await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
                var data = new Dictionary<string, object>();
                data["PlayerName"] = username;
                data["GamePlayed"] = 0;
                double winRate = 0.00;
                data["WinRate"] = winRate.ToString("P", CultureInfo.InvariantCulture);
                data["Trophies"] = 1000;
                await CloudSaveService.Instance.Data.Player.SaveAsync(data);
                UIManager.instance.CloseLoadingScreen();
                Debug.LogFormat("Sign Up is successful.");
                UIManager.instance.WelcomeScreen();
                welcomeText.text = "Welcome " + username;
            }
            catch(AuthenticationException ex)
            {
               Debug.LogException(ex);
               UIManager.instance.CloseLoadingScreen();
            }
            catch (RequestFailedException ex)
            {
                Debug.LogException(ex);
                UIManager.instance.CloseLoadingScreen();
            }
        }   
    }
    
}
