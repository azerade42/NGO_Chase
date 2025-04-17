using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class LobbyManager : NetworkBehaviour
{
    public static Action<int, PlayerController> OnPlayerAddedToLobby;
    public static Action OnAllPlayersMoved;
    [SerializeField] private int _maxPlayerCount;
    [SerializeField] Transform [] _startingTransforms;

    
    public static int _playerCount = 0;
    private PlayerController[] _playersInLobby;

    private void Awake()
    {
        _playersInLobby = new PlayerController[_maxPlayerCount];
    }

    public override void OnNetworkSpawn()
    {
        if (!HasAuthority)
            return;

        PlayerController.OnSpawned += AddPlayerToLobby;
        RoundManager.OnHidePhaseStarted += MoveLobbyToPositions;
    }

    public override void OnNetworkDespawn()
    {
        if (!HasAuthority)
            return;
        
        PlayerController.OnSpawned -= AddPlayerToLobby;
        RoundManager.OnHidePhaseStarted -= MoveLobbyToPositions;
    }

    private void AddPlayerToLobby(PlayerController player)
    {
        if (++_playerCount > _maxPlayerCount)
            return;
        
        print($"player {_playerCount} added to lobby");

        _playersInLobby[_playerCount - 1] = player;

        int randomPos = UnityEngine.Random.Range(0, _startingTransforms.Length);
        Vector3 newPos = _startingTransforms[randomPos].position + Vector3.up * 2f;
        player.ServerTeleportRpc(newPos);
        
        OnPlayerAddedToLobby?.Invoke(_playerCount, player);
    }

    private void MoveLobbyToPositions()
    {
        Quaternion hostRotation = _playersInLobby[0].transform.rotation;
        for (int i = 0; i < _playerCount; i++)
        {
            Vector3 newPos = _startingTransforms[i].position;
            _playersInLobby[i].ServerTeleportRpc(newPos);
            _playersInLobby[i].ServerRotateRpc(hostRotation);
        }
    }
}
