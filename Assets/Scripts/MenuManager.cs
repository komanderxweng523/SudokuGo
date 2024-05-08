using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Services.CloudSave;
using System.Threading.Tasks;

public class MenuManager : MonoBehaviour
{
    public static MenuManager instance;

    public Text playerName;
    public Text trophy;
    public GameObject profileUI;
    public GameObject settingsUI;
    public GameObject achievementsUI;
    public GameObject leaderboardUI;
    public GameObject searchUI;

    private void Awake()
    {
        LoadData();
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

    public void OpenProfile()
    {
        profileUI.SetActive(true);
    }

    public void CloseProfile()
    {
        profileUI.SetActive(false);
    }

    public void OpenSettings()
    {
        settingsUI.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsUI.SetActive(false);
    }

    public void OpenAchievements()
    {
        achievementsUI.SetActive(true);
    }

    public void CloseAchievements()
    {
        achievementsUI.SetActive(false);
    }

    public void OpenLeaderboard()
    {
        leaderboardUI.SetActive(true);
    }

    public void CloseLeaderboard()
    {
        leaderboardUI.SetActive(false);
    }

    public void LoadSceneMultiplayer()
    {
        SceneManager.LoadScene("MultiGame");
    }

    public void LoadSceneSingleplayer()
    {
        SceneManager.LoadScene("SingleGame");
    }

    async void LoadData()
    {
        var playerData = await CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> {
            "PlayerName", "Trophies"
        });

        if(playerData.TryGetValue("PlayerName", out var _playerName))
        {
            playerName.text = _playerName;
        }
        if(playerData.TryGetValue("Trophies", out var _trophy))
        {
            trophy.text = _trophy;
        }
    }

    public void StartSearch()
    {
        searchUI.SetActive(true);
    }

    public void CancelSearch()
    {
        searchUI.SetActive(false);
        Timer.instance.OnCancelSearch();
    }
}
