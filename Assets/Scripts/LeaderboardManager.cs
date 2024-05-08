using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;

public class LeaderboardManager : MonoBehaviour
{
    //[HideInInspector] public PlayerControls playerScript;

    [SerializeField] private GameObject leaderboardParent;
    [SerializeField] private Transform leaderboardContentParent;
    [SerializeField] private Transform leaderboardItemPrefab;

    private string leaderboardID = "Ranked_Leaderboard";
    // Start is called before the first frame update
    private async void Start()
    {
        await UnityServices.InitializeAsync();
        await SignInCachedUserAsync();

        LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardID, 0);
        leaderboardParent.SetActive(false);
    }

    async Task SignInCachedUserAsync()
    {
        // Check if a cached player already exists by checking if the session token exists
        if (AuthenticationService.Instance.SessionTokenExists) 
        {
            Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId}"); 
            return;
        }

        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Sign in anonymously succeeded!");
            Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId}");   
        }
        catch (AuthenticationException ex)
        {
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            Debug.LogException(ex);
        }  
    }

    // Update is called once per frame
    private async void Update()
    {
        if(leaderboardParent.activeInHierarchy)
        { 
            leaderboardParent.SetActive(false);
        }
        else
        {
            leaderboardParent.SetActive(true);
            UpdateLeaderboard();
        }
    }

    private async void UpdateLeaderboard()
    {
        while(Application.isPlaying && leaderboardParent.activeInHierarchy)
        {
            LeaderboardScoresPage leaderboardScoresPage = await LeaderboardsService.Instance.GetScoresAsync(leaderboardID);

            foreach(Transform t in leaderboardContentParent)
            {
                Destroy(t.gameObject);
            }

            foreach (LeaderboardEntry entry in leaderboardScoresPage.Results)
            {
                Transform leaderboardItem = Instantiate(leaderboardItemPrefab, leaderboardContentParent);
                leaderboardItem.GetChild(0).GetComponent<Text>().text = entry.PlayerName;
                leaderboardItem.GetChild(1).GetComponent<Text>().text = entry.Score.ToString();
            }


            await Task.Delay(500);
        }
    }


}
