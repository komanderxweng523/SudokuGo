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

    public void LoadScene()
    {
        SceneManager.LoadScene("Game");
    }

    async void LoadData()
    {
        Dictionary<string, string> PlayerNameData = await CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> { "PlayerName" });
        playerName.text = PlayerNameData["PlayerName"];

        Dictionary<string, string> PlayerTrophyData = await CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> { "Trophies" });
        trophy.text = PlayerTrophyData["Trophies"];
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
