using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.CloudSave;
using Unity.Netcode;

public class Player : MonoBehaviour
{
    public string username;
    public int gamePlayed;
    public float winRate;
    public int trophies;

    async void SaveData()
    {
        var saveData = new Dictionary<string, object>();
        saveData["PlayerName"] = username;
        saveData["GamePlayed"] = gamePlayed;
        saveData["WinRate"] = winRate;
        saveData["Trophies"] = trophies;
        await CloudSaveService.Instance.Data.Player.SaveAsync(saveData);
    }
}
