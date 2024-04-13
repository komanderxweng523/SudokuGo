using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Multiplay;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class MultiplayManager : MonoBehaviour
{
    private IServerQueryHandler serverQueryHandler;
    private async void Start()
    {
        if(Application.platform == RuntimePlatform.LinuxServer)
        {
            Application.targetFrameRate = 60;
            await UnityServices.InitializeAsync();
            ServerConfig serverConfig = MultiplayService.Instance.ServerConfig;
            serverQueryHandler = await MultiplayService.Instance.StartServerQueryHandlerAsync(2, "SudokuServer", "Sudoku", "75557", "0");
            if(serverConfig.AllocationId != string.Empty)
            {
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData("0.0.0.0", serverConfig.Port, "0.0.0.0");
                NetworkManager.Singleton.StartServer();
                await MultiplayService.Instance.ReadyServerForPlayersAsync();
            }
        }
    }

    private async void Update()
    {
        if(Application.platform == RuntimePlatform.LinuxServer)
        {
            if(serverQueryHandler != null)
            {
                serverQueryHandler.CurrentPlayers = (ushort)NetworkManager.Singleton.ConnectedClientsIds.Count;
                serverQueryHandler.UpdateServerCheck();
                await Task.Delay(100);
            }
        }
    }

    public void JoinToServer()
    {
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData("127.0.0.1", (ushort)7777);
        NetworkManager.Singleton.StartClient();
    }
}
