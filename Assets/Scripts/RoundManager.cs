using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class RoundManager : NetworkBehaviour
{
    public static Action<string> OnRoundMessageChangedRpc;
    public static Action OnHidePhaseStarted;
    public static Action OnChasePhaseStarted;
    public static Action OnRoundEndedRpc;

    private readonly string LOBBY_MESSAGE = "Waiting for players...";
    private readonly string HOST_CHASE_PHASE_MESSAGE = "You're being chased!";
    private readonly string CLIENT_CHASE_PHASE_MESSAGE = "Catch the red guy!";
    private readonly string HOST_HIDE_PHASE_MESSAGE = "Run and hide!";
    private readonly string CLIENT_HIDE_PHASE_MESSAGE = "Hold on!";
    private readonly string HOST_WIN_MESSAGE = "Red guy wins!";
    private readonly string CLIENT_WIN_MESSAGE = "The red guy has been caught!";

    private Coroutine _countdownRoutine;
    private bool _roundIsActive;
    public bool _roundCount { get; private set; }
    

    [SerializeField] int _roundStartCountdownTime;
    [SerializeField] int _hideCountdownTime;
    [SerializeField] int _chaseCountdownTime;

    public override void OnNetworkSpawn()
    {
        if (!HasAuthority)  
            return;

        LobbyManager.OnPlayerAddedToLobby += BeginRound;
        PlayerController.OnTouchedAnotherPlayer += ClientWonRound;
    }

    public override void OnNetworkDespawn()
    {
        if (!HasAuthority)  
            return;

        LobbyManager.OnPlayerAddedToLobby -= BeginRound;
        PlayerController.OnTouchedAnotherPlayer -= ClientWonRound;
    }

    private void Start()
    {
        OnRoundMessageChangedRpc?.Invoke(LOBBY_MESSAGE);
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
        OnRoundMessageChangedRpc?.Invoke(countdownTime.ToString());
    }

    private void BeginRound(int playerCount)
    {
        if (_roundIsActive)
            return;
        
        // start the countdown if 2 or more players have joined
        if (playerCount >= 2 && _countdownRoutine == null)
        {
            Action OnCountdownEnd = BeginHidePhase;
            _countdownRoutine = StartCoroutine(BeginCountdown(0, _roundStartCountdownTime, OnCountdownEnd));
        }
        // stop the countdown if less than 2 players are in the lobby
        else if (playerCount < 2 && _countdownRoutine != null)
        {
            StopCoroutine(_countdownRoutine);
        }
    }

    private void BeginHidePhase()
    {
        OnHidePhaseStarted?.Invoke();

        HidePhaseStartRpc();
        Action OnCountdownEnd = BeginChasePhase;
        _countdownRoutine = StartCoroutine(BeginCountdown(3, _hideCountdownTime - 3, OnCountdownEnd));
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void HidePhaseStartRpc()
    {
        if (NetworkManager.Singleton.LocalClientId == 0)
            OnRoundMessageChangedRpc?.Invoke(HOST_HIDE_PHASE_MESSAGE);
        else
            OnRoundMessageChangedRpc?.Invoke(CLIENT_HIDE_PHASE_MESSAGE);
    }

    private void BeginChasePhase()
    {
        _roundIsActive = true;
        OnChasePhaseStarted?.Invoke();
        ChaseStartRpc();

        Action OnCountdownEnd = () => { EndRound(true); };
        _countdownRoutine = StartCoroutine(BeginCountdown(3, _chaseCountdownTime - 3, OnCountdownEnd));
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ChaseStartRpc()
    {
        if (NetworkManager.Singleton.LocalClientId == 0)
            OnRoundMessageChangedRpc?.Invoke(HOST_CHASE_PHASE_MESSAGE);
        else
            OnRoundMessageChangedRpc?.Invoke(CLIENT_CHASE_PHASE_MESSAGE);
    }

    private void ClientWonRound() => EndRound(false);
    private void EndRound(bool hostWon)
    {
        if (!_roundIsActive)
            return;
        
        _roundIsActive = false;

        if (_countdownRoutine != null)
            StopCoroutine(_countdownRoutine);
        
        Invoke(nameof(RestartRound), 5f);
        
        EndRoundRpc(hostWon);
    }

    private void RestartRound()
    {
        _countdownRoutine = null;
        BeginRound(LobbyManager._playerCount);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void EndRoundRpc(bool hostWon)
    {
        OnRoundEndedRpc?.Invoke();

        if (hostWon)
            OnRoundMessageChangedRpc?.Invoke(HOST_WIN_MESSAGE);
        else
            OnRoundMessageChangedRpc?.Invoke(CLIENT_WIN_MESSAGE);
    }   
}
