using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class LobbyManager : NetworkBehaviour
{
    public static Action<ulong> OnPlayerAddedToLobby;
    public static Action OnAllPlayersMoved;

    [SerializeField] private int _maxPlayerCount;
    [SerializeField] private Transform [] _startingTransforms;
    
    public static int PlayerCount = 0;
    private PlayerController[] _playersInLobby;
    private NetworkSpawnManager networkSpawnManager;

    private void Awake()
    {
        _playersInLobby = new PlayerController[_maxPlayerCount];
    }

    public override void OnNetworkSpawn()
    {
        if (!HasAuthority)
            return;
            
        networkSpawnManager = NetworkManager.Singleton.SpawnManager;

        NetworkManager.Singleton.OnClientConnectedCallback += AddPlayerToLobby;
        NetworkManager.Singleton.OnClientDisconnectCallback += RemovePlayerFromLobby;
        
        RoundManager.OnHidePhaseStarted += MoveLobbyToPositions;
    }

    public override void OnNetworkDespawn()
    {
        if (!HasAuthority)
            return;
        
        NetworkManager.Singleton.OnClientConnectedCallback -= AddPlayerToLobby;
        NetworkManager.Singleton.OnClientDisconnectCallback -= RemovePlayerFromLobby;
        
        RoundManager.OnHidePhaseStarted -= MoveLobbyToPositions;
    }

    private void AddPlayerToLobby(ulong clientID)
    {
        if (PlayerCount + 1 > _maxPlayerCount)
            return;

        NetworkObject playerObj = networkSpawnManager.GetPlayerNetworkObject(clientID);
        PlayerController player = playerObj.GetComponent<PlayerController>();

        if (player == null)
            return;

        _playersInLobby[PlayerCount] = player;
        PlayerCount++;

        print($"player {PlayerCount} added to lobby");

        int randomPos = UnityEngine.Random.Range(0, _startingTransforms.Length);
        Vector3 newPos = _startingTransforms[randomPos].position + Vector3.up * 2f;
        player.TeleportRpc(newPos);
        
        OnPlayerAddedToLobby?.Invoke(clientID);
    }

    private void RemovePlayerFromLobby(ulong clientID)
    {
        _playersInLobby[clientID] = null;
        PlayerCount--;
    }

    private void MoveLobbyToPositions()
    {
        Quaternion hostRotation = _playersInLobby[0].transform.rotation;
        for (int i = 0; i < PlayerCount; i++)
        {
            Vector3 newPos = _startingTransforms[i].position;
            _playersInLobby[i].TeleportRpc(newPos);
            _playersInLobby[i].RotateRpc(hostRotation);
        }
    }
}
