using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;
using System.Threading.Tasks;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using StatusOptions = Unity.Services.Matchmaker.Models.MultiplayAssignment.StatusOptions;
#if UNITY_EDITOR
using ParrelSync;
#endif

public class Client: MonoBehaviour
{
    private string ticketId;

    private void OnEnable() 
    {
        GameServer.ClientInstance += SignIn;
    }

    private void OnDisable() 
    {
        GameServer.ClientInstance -= SignIn;
    }

    private async void SignIn()
    {
        await ClientSignIn("SudokuPlayer");
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private async Task ClientSignIn(string serviceProfileName = null)
    {
        if(serviceProfileName != null)
        {
            #if UNITY_EDITOR
            serviceProfileName = $"{serviceProfileName}{GetCloneNumberSuffix()}";
            #endif
            var initOptions = new InitializationOptions();
            initOptions.SetProfile(serviceProfileName);
            await UnityServices.InitializeAsync(initOptions);
        }
        else
        {
            await UnityServices.InitializeAsync();
        }
        Debug.Log($"Signed In Anonymously As{serviceProfileName}({PlayerID()})");
    }

    private string PlayerID()
    {
        return AuthenticationService.Instance.PlayerId;
    }

    #if UNITY_EDITOR
    private string GetCloneNumberSuffix()
    {
        string projectPath = ClonesManager.GetCurrentProjectPath();
        int lastUnderScore = projectPath.LastIndexOf("_");
        string projectCloneSuffix = projectPath.Substring(lastUnderScore + 1);
        if(projectCloneSuffix.Length != 1)
            projectCloneSuffix = "";
        return projectCloneSuffix;
    }
    #endif

    public void StartClient()
    {
        CreateATicket();
    }

    private async void CreateATicket()
    {
        var options = new CreateTicketOptions("default-queue");
        var players = new List<Player>
        {
            new Player(
                PlayerID(),
                new MatchmakingPlayerData
                {
                    Skill = 100,
                }
            )
        };
        var ticketResponse = await MatchmakerService.Instance.CreateTicketAsync(players, options);   
        ticketId = ticketResponse.Id;
        Debug.Log($"Ticket ID: {ticketId}");
        PoolTicketStatus();   
    }

    private async void PoolTicketStatus()
    {
        MultiplayAssignment multiplayAssignment = null;
        bool gotAssignment = false;
        do
        {
            await Task.Delay(TimeSpan.FromSeconds(1f));
            var ticketStatus = await MatchmakerService.Instance.GetTicketAsync(ticketId);
            if(ticketStatus == null) continue;
            if(ticketStatus.Type == typeof(MultiplayAssignment))
            {
                multiplayAssignment = ticketStatus.Value as MultiplayAssignment;
            }
            switch(multiplayAssignment.Status)
            {
                case StatusOptions.Found:
                    gotAssignment = true;
                    TicketAssigned(multiplayAssignment);
                    break;
                case StatusOptions.InProgress:
                    break;
                case StatusOptions.Failed:
                    gotAssignment = true;
                    Debug.Log($"Failed to get ticket status. Error: {multiplayAssignment.Message} ");
                    break;
                case StatusOptions.Timeout:
                    gotAssignment = true;
                    Debug.Log("Failed to get ticket status. Ticket timed out.");
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }while(!gotAssignment);
    }

    private void TicketAssigned(MultiplayAssignment assignment)
    {
        Debug.Log($"Ticket Assigned: {assignment.Ip}:{assignment.Port} ");
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(assignment.Ip, (ushort)assignment.Port);
        NetworkManager.Singleton.StartClient();
    }

    [Serializable]
    public class MatchmakingPlayerData
    {
        public int Skill;
    }
}