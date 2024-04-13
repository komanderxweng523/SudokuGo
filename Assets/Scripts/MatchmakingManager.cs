using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Multiplay;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;
public class MatchmakingManager : NetworkBehaviour 
{
    private PayloadAllocation payloadAllocation;
    private IMatchmakerService matchmakerService;
    private string backfillTicketId;
    private NetworkManager networkManager;
    private string currentTicket;
    private bool isMatchmaking;
    private CancellationTokenSource cancellationTokenSource;

    private async void Start()
    {
        networkManager = NetworkManager.Singleton;
        if(Application.platform != RuntimePlatform.LinuxServer)
        {
            await UnityServices.InitializeAsync();
            await SignInCachedUserAsync();
        }
        else
        {
            while(UnityServices.State == ServicesInitializationState.Uninitialized || UnityServices.State == ServicesInitializationState.Initializing)
            {
                await Task.Yield();
            }

            matchmakerService = MatchmakerService.Instance;
            payloadAllocation = await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<PayloadAllocation>();
            backfillTicketId = payloadAllocation.BackfillTicketId;
        }
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

    bool isDeallocating = false;
    bool deallocatingCancellationToken = false;

    private async void Update()
    {
        if(Application.platform == RuntimePlatform.LinuxServer)
        {
            if(NetworkManager.Singleton.ConnectedClientsList.Count == 0 && !isDeallocating)
            {
                isDeallocating = true; 
                deallocatingCancellationToken = false;
                Deallocate();
            }
            if(NetworkManager.Singleton.ConnectedClientsList.Count != 0)
            {
                isDeallocating = false; 
                deallocatingCancellationToken = true;
            }
            if(backfillTicketId != null && NetworkManager.Singleton.ConnectedClientsList.Count < 2)
            {
                BackfillTicket backfillTicket = await MatchmakerService.Instance.ApproveBackfillTicketAsync(backfillTicketId);
                backfillTicketId = backfillTicket.Id;
            }

            await Task.Delay(1000);
        }
    }

    private void OnPlayerConnected() 
    {
        if(Application.platform == RuntimePlatform.LinuxServer)
        {
            UpdateBackfillTicket();
        }
    }

    private void OnPlayerDisconnected() 
    {
        if(Application.platform == RuntimePlatform.LinuxServer)
        {
            UpdateBackfillTicket();
        }
    }

    private async void UpdateBackfillTicket()
    {
        List<Unity.Services.Matchmaker.Models.Player> players = new List<Unity.Services.Matchmaker.Models.Player>();
        foreach(ulong playerId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            players.Add(new Unity.Services.Matchmaker.Models.Player(playerId.ToString()));
        }

        MatchProperties matchProperties = new MatchProperties(null, players, null, backfillTicketId);
        await MatchmakerService.Instance.UpdateBackfillTicketAsync(payloadAllocation.BackfillTicketId,
            new BackfillTicket(backfillTicketId, properties: new BackfillTicketProperties(matchProperties))); 
    }

    private void OnApplicationQuit() 
    {
        if(Application.platform != RuntimePlatform.LinuxServer)
        {
            if(networkManager.IsConnectedClient)
            {
                networkManager.Shutdown(true);
                networkManager.DisconnectClient(OwnerClientId);
            }
        }
    }

    private async void Deallocate()
    {
        await Task.Delay(60 * 1000);

        if(!deallocatingCancellationToken)
        {
            Application.Quit();
        }
    }

    public async void ClientJoin()
    {
        if (isMatchmaking)
        {
            Debug.Log("Already searching for a match.");
            return;
        }

        isMatchmaking = true;
        cancellationTokenSource = new CancellationTokenSource();

        CreateTicketOptions createTicketOptions = new CreateTicketOptions("default-queue");
        List<Unity.Services.Matchmaker.Models.Player> players = new List<Unity.Services.Matchmaker.Models.Player>{new Unity.Services.Matchmaker.Models.Player(AuthenticationService.Instance.PlayerId)};
        CreateTicketResponse createTicketResponse = await MatchmakerService.Instance.CreateTicketAsync(players, createTicketOptions);
        currentTicket = createTicketResponse.Id;
        Debug.Log("Ticket created");
        MenuManager.instance.StartSearch();

        while(isMatchmaking)
        {
            TicketStatusResponse ticketStatusResponse = await MatchmakerService.Instance.GetTicketAsync(createTicketResponse.Id);
            if(ticketStatusResponse.Type == typeof(MultiplayAssignment))
            {
                MultiplayAssignment multiplayAssignment =(MultiplayAssignment)ticketStatusResponse.Value;

                if(multiplayAssignment.Status == MultiplayAssignment.StatusOptions.Found)
                {
                    UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                    transport.SetConnectionData(multiplayAssignment.Ip, ushort.Parse(multiplayAssignment.Port.ToString()));
                    NetworkManager.Singleton.StartClient();

                    Debug.Log("Match found");
                    MenuManager.instance.LoadScene();
                    isMatchmaking = false;
                    return;
                }
                else if (multiplayAssignment.Status == MultiplayAssignment.StatusOptions.Timeout)
                {
                    Debug.Log("Match timeout");
                    isMatchmaking = false;
                    CancelMatchmaking();
                }
                else if (multiplayAssignment.Status == MultiplayAssignment.StatusOptions.Failed)
                {
                    Debug.Log("Match " + multiplayAssignment.Status + " " + multiplayAssignment.Message);
                    isMatchmaking = false;
                    CancelMatchmaking();
                }
                else if (multiplayAssignment.Status == MultiplayAssignment.StatusOptions.InProgress)
                {
                    Debug.Log("Match is in progress");
 
                }
            }
            await Task.Delay(1000);
        }
    }

    public void CancelMatchmaking()
    {
        cancellationTokenSource.Cancel();
        MenuManager.instance.CancelSearch();
    }

    [System.Serializable]
    public class PayloadAllocation
    {
        public MatchProperties MatchProperties;
        public string GeneratorName;
        public string QueueName;
        public string PoolName;
        public string EnvironmentId;
        public string BackfillTicketId;
        public string MatchId;
        public string PoolId;
    }
}