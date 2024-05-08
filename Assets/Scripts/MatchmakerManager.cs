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
using Unity.Services.Multiplay.Models;
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

        if (Application.platform != RuntimePlatform.LinuxServer)
        {
            await UnityServices.InitializeAsync();
            await SignInCachedUserAsync();
        }
        else
        {
            while (UnityServices.State == ServicesInitializationState.Uninitialized || UnityServices.State == ServicesInitializationState.Initializing)
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
        if (Application.platform == RuntimePlatform.LinuxServer)
        {
            if (NetworkManager.Singleton.ConnectedClientsList.Count == 0 && !isDeallocating)
            {
                isDeallocating = true;
                deallocatingCancellationToken = false;
                Deallocate();
            }

            if (NetworkManager.Singleton.ConnectedClientsList.Count != 0)
            {
                isDeallocating = false;
                deallocatingCancellationToken = true;
            }

            if (backfillTicketId != null && NetworkManager.Singleton.ConnectedClientsList.Count < 2)
            {
                BackfillTicket backfillTicket = await MatchmakerService.Instance.ApproveBackfillTicketAsync(backfillTicketId);
                backfillTicketId = backfillTicket.Id;
            }

            await Task.Delay(1000);
        }
    }

    private void LogNetworkEvent(string eventName, string message)
    {
        Debug.Log($"[Network] {eventName}: {message}");
    }

    private void OnPlayerConnected(ulong clientId)
    {
        if (Application.platform == RuntimePlatform.LinuxServer)
        {
            UpdateBackfillTicket();
        }
        LogNetworkEvent("Client Connected", $"Client ID: {clientId}");
    }
    private void OnPlayerDisconnected(ulong clientId)
    {
        if (Application.platform == RuntimePlatform.LinuxServer)
        {
            UpdateBackfillTicket();
        }
        LogNetworkEvent("Client Disconnected", $"Client ID: {clientId}");
    }

    private async void UpdateBackfillTicket()
    {
        List<Player> players = new List<Player>();

        foreach (ulong playerId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            players.Add(new Player(playerId.ToString()));
        }

        MatchProperties matchProperties = new MatchProperties(null, players, null, backfillTicketId);

        await MatchmakerService.Instance.UpdateBackfillTicketAsync(payloadAllocation.BackfillTicketId,
            new BackfillTicket(backfillTicketId, properties: new BackfillTicketProperties(matchProperties)));
    }


    private async void Deallocate()
    {
        await Task.Delay(60 * 1000);

        if (!deallocatingCancellationToken)
        {
            Application.Quit();
        }
    }

    private void OnApplicationQuit()
    {
        if (Application.platform != RuntimePlatform.LinuxServer)
        {
            if (networkManager.IsConnectedClient)
            {
                networkManager.Shutdown(true);
                networkManager.DisconnectClient(OwnerClientId);
            }
        }
    }

    private void LogTicketStatus(TicketStatusResponse response)
    {
        Debug.Log($"[Matchmaker] Ticket Status: {response.Type}, Status: {response.Value}");

        // Log additional information depending on the response type
        if (response.Type == typeof(MultiplayAssignment))
        {
            var assignment = (MultiplayAssignment)response.Value;
            Debug.Log($"[Matchmaker] - Assignment Type: {assignment.AssignmentType}");
            Debug.Log($"[Matchmaker] - Server IP: {assignment.Ip}");
            Debug.Log($"[Matchmaker] - Server Port: {string.Join(", ", assignment.Port)}");
        }
    }

    public async void ClientJoin()
    {
        isMatchmaking = true;
        cancellationTokenSource = new CancellationTokenSource();

        CreateTicketOptions createTicketOptions = new CreateTicketOptions("default-queue",
            new Dictionary<string, object>{});
        Debug.Log(createTicketOptions.ToString());
        List<Player> players = new List<Player> { new Player(AuthenticationService.Instance.PlayerId) };
        foreach (Player player in players)
        {
            Debug.Log(player);
        }
        CreateTicketResponse createTicketResponse = await MatchmakerService.Instance.CreateTicketAsync(players, createTicketOptions);
        currentTicket = createTicketResponse.Id;
        Debug.Log("Ticket created");
        MenuManager.instance.StartSearch();
        Debug.Log(currentTicket + " " + isMatchmaking + " " + cancellationTokenSource.Token);

        while (isMatchmaking && !cancellationTokenSource.IsCancellationRequested)
        {
            TicketStatusResponse ticketStatusResponse = await MatchmakerService.Instance.GetTicketAsync(createTicketResponse.Id);
            LogTicketStatus(ticketStatusResponse);
            if (ticketStatusResponse.Type == typeof(MultiplayAssignment))
            {
                MultiplayAssignment multiplayAssignment = (MultiplayAssignment)ticketStatusResponse.Value;
                Debug.Log($"[Matchmaker] Match Assignment Ticket Status: {multiplayAssignment.Status}");
                if (multiplayAssignment.Status == MultiplayAssignment.StatusOptions.Found)
                {
                    UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                    transport.SetConnectionData(multiplayAssignment.Ip, ushort.Parse(multiplayAssignment.Port.ToString()));
                    NetworkManager.Singleton.StartClient();

                    Debug.Log("Match found");
                    MenuManager.instance.LoadSceneMultiplayer();
                    return;
                }
                else if (multiplayAssignment.Status == MultiplayAssignment.StatusOptions.Timeout)
                {
                    Debug.Log("Match timeout");
                    MenuManager.instance.CancelSearch();
                    return;
                }
                else if (multiplayAssignment.Status == MultiplayAssignment.StatusOptions.Failed)
                {
                    Debug.Log("Match failed" + multiplayAssignment.Status + "  " + multiplayAssignment.Message);
                    MenuManager.instance.CancelSearch();
                    return;
                }
                else if (multiplayAssignment.Status == MultiplayAssignment.StatusOptions.InProgress)
                {
                    Debug.Log("Match is in progress");
                }

            }

            await Task.Delay(1000);
        }

    }

    public async Task CancelMatchmaking()
    {
        if (!isMatchmaking)
            return;
        isMatchmaking = false;
        if (cancellationTokenSource.Token.CanBeCanceled)
            cancellationTokenSource.Cancel();
        if (string.IsNullOrEmpty(currentTicket))
            return;
        Debug.Log($"Cancelling {currentTicket}");
        try
        {
            await MatchmakerService.Instance.DeleteTicketAsync(currentTicket);
        }
        catch(RequestFailedException ex)
        {
            Debug.LogError($"DeleteTicketAsync failed: {ex.Message}");
        }   
    }

    public void Dispose()
    {
        #pragma warning disable 4014
            CancelMatchmaking();
        #pragma warning restore 4014
        cancellationTokenSource?.Dispose();
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