using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;

public class PlayerData : MonoBehaviour
{
    public Text playerName;
    public Text gamePlayed;
    public Text winRate;

    // Start is called before the first frame update
    void Start()
    {
        LoadPlayerData();
    }

    async void LoadPlayerData()
    {
        var playerData = await CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> {
            "PlayerName", "GamePlayed", "WinRate"
        });

        if(playerData.TryGetValue("PlayerName", out var _playerName))
        {
            playerName.text = _playerName;
        }
        if(playerData.TryGetValue("GamePlayed", out var _gamePlayed))
        {
            gamePlayed.text = _gamePlayed;
        }
        if(playerData.TryGetValue("WinRate", out var _winRate))
        {
            winRate.text = _winRate;
        }
    }

}
