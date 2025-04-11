using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class RoundManager : NetworkBehaviour
{
    public static Action<string> OnCountdownMessageChangedRpc;
    public static Action OnHidePhaseStarted;
    public static Action OnChasePhaseStarted;
    public static Action OnRoundEndedRpc;

    private const string HOST_CHASE_PHASE_MESSAGE = "You're being chased!";
    private const string CLIENT_CHASE_PHASE_MESSAGE = "Catch the red guy!";
    private const string HOST_HIDE_PHASE_MESSAGE = "Run and hide!";
    private const string CLIENT_HIDE_PHASE_MESSAGE = "Hold on!";
    private const string ROUND_OVER_MESSAGE = "Round completed!";

    private Coroutine countdownRoutine;
    private bool _roundIsActive;
    

    [SerializeField] int _roundStartCountdownTime;
    [SerializeField] int _hideCountdownTime;
    [SerializeField] int _chaseCountdownTime;

    public override void OnNetworkSpawn()
    {
        if (!HasAuthority)  
            return;

        LobbyManager.OnPlayerAddedToLobby += BeginRound;
        PlayerController.OnTouchedAnotherPlayer += EndRound;
    }

    public override void OnNetworkDespawn()
    {
        if (!HasAuthority)  
            return;

        LobbyManager.OnPlayerAddedToLobby -= BeginRound;
        PlayerController.OnTouchedAnotherPlayer -= EndRound;
    }

    private IEnumerator BeginCountdown(float delayStart, int timeRemaining, Action EndAction)
    {
        yield return new WaitForSeconds(delayStart);

        UpdateCountdownTimeRpc(timeRemaining);

        while (timeRemaining > 0)
        {
            yield return new WaitForSeconds(1);
            UpdateCountdownTimeRpc(--timeRemaining);
        }

        EndAction?.Invoke();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void UpdateCountdownTimeRpc(int countdownTime)
    {
        OnCountdownMessageChangedRpc?.Invoke(countdownTime.ToString());
    }

    private void BeginRound(int playerCount)
    {
        if (_roundIsActive)
            return;
        
        // start the countdown if 2 or more players have joined
        if (playerCount >= 2 && countdownRoutine == null)
        {
            Action OnCountdownEnd = BeginHidePhase;
            countdownRoutine = StartCoroutine(BeginCountdown(0, _roundStartCountdownTime, OnCountdownEnd));
        }
        // stop the countdown if less than 2 players are in the lobby
        else if (playerCount < 2 && countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
        }
    }

    private void BeginHidePhase()
    {
        OnHidePhaseStarted?.Invoke();

        HidePhaseStartRpc();
        Action OnCountdownEnd = BeginChasePhase;
        countdownRoutine = StartCoroutine(BeginCountdown(3, _hideCountdownTime - 3, OnCountdownEnd));
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void HidePhaseStartRpc()
    {
        if (NetworkManager.Singleton.LocalClientId == 0)
            OnCountdownMessageChangedRpc?.Invoke(HOST_HIDE_PHASE_MESSAGE);
        else
            OnCountdownMessageChangedRpc?.Invoke(CLIENT_HIDE_PHASE_MESSAGE);
    }

    private void BeginChasePhase()
    {
        _roundIsActive = true;
        OnChasePhaseStarted?.Invoke();
        ChaseStartRpc();

        Action OnCountdownEnd = () => { EndRound(0); };
        countdownRoutine = StartCoroutine(BeginCountdown(3, _chaseCountdownTime - 3, OnCountdownEnd));
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ChaseStartRpc()
    {
        if (NetworkManager.Singleton.LocalClientId == 0)
            OnCountdownMessageChangedRpc?.Invoke(HOST_CHASE_PHASE_MESSAGE);
        else
            OnCountdownMessageChangedRpc?.Invoke(CLIENT_CHASE_PHASE_MESSAGE);
    }

    private void EndRound(ulong clientID)
    {
        if (!_roundIsActive)
            return;
        
        _roundIsActive = false;

        if (countdownRoutine != null)
            StopCoroutine(countdownRoutine);

        EndRoundRpc();
        Debug.Log($"Client {clientID} is the winner!");

        Invoke(nameof(RestartRound), 5f);
    }

    private void RestartRound()
    {
        BeginRound(2);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void EndRoundRpc()
    {
        OnRoundEndedRpc?.Invoke();
        OnCountdownMessageChangedRpc?.Invoke(ROUND_OVER_MESSAGE);
    }   
}
