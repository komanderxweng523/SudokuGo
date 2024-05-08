using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Multiplay;
public class GameServer : MonoBehaviour 
{

    public static event System.Action ClientInstance;
    private const string InternalServerIP = "0.0.0.0";
    private string externalServerIP = "0.0.0.0";
    private ushort _serverPort = 7777;

    private string externalConnectionString => $"{externalServerIP}:{_serverPort}";
    
    private IMultiplayService multiplayService;
    private const int multiplayServiceTimeout = 20000;

    private string allocationId;
    private MultiplayEventCallbacks serverCallbacks;
    private IServerEvents serverEvents;

    private BackfillTicket localBackfillTicket;
    private CreateBackfillTicketOptions createBackfillTicketOptions;
    private const int ticketCheckMs = 1000;
    private MatchmakingResults matchmakingPayload;

    private bool backfilling = false;
    async void Start() 
    {
        bool server = false;    
        var args = System.Environment.GetCommandLineArgs();
        for(int i = 0; i < args.Length; i++)
        {
            if(args[i] == "-dedicatedServer")
            {
                server = true;
            }

            if(args[i] == "-port" && (i + 1 < args.Length))
            {
                _serverPort = (ushort)int.Parse(args[i + 1]);
            }

            if(args[i] == "-ip" && (i + 1 < args.Length))
            {
                externalServerIP = args[i + 1];
            }
        }

        if(server)
        {
            StartServer();
            await StartServerServices();
        }
        else
        {
            ClientInstance?.Invoke();
        }
    }

    private void StartServer()
    {
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(InternalServerIP, _serverPort);
        NetworkManager.Singleton.StartServer();
        NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;
    }

    async Task StartServerServices()
    {
        await UnityServices.InitializeAsync();
        try
        {
            multiplayService = MultiplayService.Instance;
            await multiplayService.StartServerQueryHandlerAsync(2, "n/a", "n/a", "0", "n/a");
        }
        catch(Exception ex)
        {
            Debug.LogWarning($"Failed to set up SQP service:\n {ex}");
        }

        try
        {
            matchmakingPayload = await GetMatchmakerPayload(multiplayServiceTimeout);
            if(matchmakingPayload != null)
            {
                Debug.Log($"Got Payload: {matchmakingPayload}");
                await StartBackfill(matchmakingPayload);
            }
            else
            {
                Debug.LogWarning("Getting the Matchmaker Payload timed out");
            }
        }
        catch(Exception ex)
        {
            Debug.LogWarning($"Failed to set up Allocation & Backfill:\n {ex}");
        }
    }

    private async Task<MatchmakingResults> GetMatchmakerPayload(int timeout)
    {
        var matchmakerPayloadTask = SubscribeAndAwaitMatchmakerAllocation();
        if(await Task.WhenAny(matchmakerPayloadTask, Task.Delay(timeout)) == matchmakerPayloadTask)
        {
            return matchmakerPayloadTask.Result;
        }

        return null;
    }

    private async Task<MatchmakingResults> SubscribeAndAwaitMatchmakerAllocation()
    {
        if(multiplayService == null) return null;
        allocationId = null;
        serverCallbacks = new MultiplayEventCallbacks();
        serverCallbacks.Allocate += OnMultiplayAllocation;
        serverEvents = await multiplayService.SubscribeToServerEventsAsync(serverCallbacks);

        allocationId = await AwaitAllocationId();
        var mmPayload = await GetMatchmakerAllocationPayloadAsync();
        return mmPayload;
    }
    
    private void OnMultiplayAllocation(MultiplayAllocation allocation)
    {
        Debug.Log($"OnAllocation: {allocation.AllocationId}");
        if(string.IsNullOrEmpty(allocation.AllocationId)) return;
        allocationId = allocation.AllocationId;
    }

    private async Task<string> AwaitAllocationId()
    {
        var config = multiplayService.ServerConfig;
        Debug.Log($"Waiting Allocation. Server Config is:\n" + $"-ServerID: {config.ServerId}\n" +
                  $"-AllocationID: {config.AllocationId}\n" +
                  $"-Port: {config.Port}\n" +
                  $"-QPort: {config.QueryPort}\n" +
                  $"-logs: {config.ServerLogDirectory}");
        while(string.IsNullOrEmpty(allocationId))
        {
            var configId = config.AllocationId;
            if(!string.IsNullOrEmpty(configId) && string.IsNullOrEmpty(allocationId))
            {
                allocationId = configId;
                break;
            }
            await Task.Delay(100);
        }
        return allocationId;
    }

    private async Task<MatchmakingResults> GetMatchmakerAllocationPayloadAsync()
    {
        try
        {
            var payloadAllocation = await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<MatchmakingResults>();
            var modelAsJson = JsonConvert.SerializeObject(payloadAllocation, Formatting.Indented);
            Debug.Log($"{nameof(GetMatchmakerAllocationPayloadAsync)}:\n{modelAsJson}");
            return payloadAllocation;
        }
        catch(Exception ex)
        {
            Debug.LogWarning($"Failed to get Matchmaker Payload:\n {ex}");
        }
        return null;
    }

    private async Task StartBackfill(MatchmakingResults payload)
    {
        var backfillProperties = new BackfillTicketProperties(payload.MatchProperties);
        localBackfillTicket = new BackfillTicket{Id = payload.MatchProperties.BackfillTicketId, Properties = backfillProperties};
        await BeginBackfilling(payload);
    }

    private async Task BeginBackfilling(MatchmakingResults payload)
    {
        var matchProperties = payload.MatchProperties;
        
        if(string.IsNullOrEmpty(localBackfillTicket.Id))
        {
            createBackfillTicketOptions = new CreateBackfillTicketOptions
            {
                Connection = externalConnectionString,
                QueueName = payload.QueueName,
                Properties = new BackfillTicketProperties(matchProperties)
            };
            localBackfillTicket.Id = await MatchmakerService.Instance.CreateBackfillTicketAsync(createBackfillTicketOptions);
        }
        backfilling = true;

        #pragma warning disable 4014
        BackfillLoop();
        #pragma warning restore 4014
    }

    private async Task BackfillLoop()
    {
        while(backfilling && NeedsPlayers())
        {
            localBackfillTicket = await MatchmakerService.Instance.ApproveBackfillTicketAsync(localBackfillTicket.Id);
            if(!NeedsPlayers())
            {
                await MatchmakerService.Instance.DeleteBackfillTicketAsync(localBackfillTicket.Id);
                localBackfillTicket.Id = null;
                backfilling = false;
                return;
            }
            await Task.Delay(ticketCheckMs);
        }
        backfilling = false;
    }

    private void ClientDisconnected(ulong clientId)
    {
        if(!backfilling && NetworkManager.Singleton.ConnectedClients.Count > 0 && NeedsPlayers())
        {
            BeginBackfilling(matchmakingPayload);
        }
    }

    private bool NeedsPlayers()
    {
        return NetworkManager.Singleton.ConnectedClients.Count < 2;
    }

    private void Dispose()
    {
        serverCallbacks.Allocate -= OnMultiplayAllocation;
        serverEvents?.UnsubscribeAsync();
    }
}