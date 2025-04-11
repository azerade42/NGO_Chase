using System;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;

public class LobbyManager : NetworkBehaviour
{
    public static Action<int> OnPlayerAddedToLobby;
    public static Action OnAllPlayersMoved;

    private Transform[] _playersInLobby;
    private int _playerCount = 0;
    [SerializeField] private int _maxPlayerCount;

    [SerializeField] Transform hostTransform;
    [SerializeField] Transform p1Transform;
    [SerializeField] Transform p2Transform;
    [SerializeField] Transform p3Transform;

    private void Awake()
    {
        _playersInLobby = new Transform[_maxPlayerCount];   
    }

    public override void OnNetworkSpawn()
    {
        if (!HasAuthority)
            return;
        
        PlayerController.OnSpawnedServer += AddPlayerToLobby;
        RoundManager.OnHidePhaseStarted += MoveLobbyToPositionsRpc;
    }

    public override void OnNetworkDespawn()
    {
        if (!HasAuthority)
            return;
        
        PlayerController.OnSpawnedServer -= AddPlayerToLobby;
        RoundManager.OnHidePhaseStarted -= MoveLobbyToPositionsRpc;
    }

    private void AddPlayerToLobby(Transform player)
    {
        _playersInLobby[_playerCount++] = player;
        OnPlayerAddedToLobby?.Invoke(_playerCount);
    }

    [Rpc(SendTo.Everyone)]
    private void MoveLobbyToPositionsRpc()
    {
        Transform transformToUse;
        ulong id = NetworkManager.Singleton.LocalClientId;

        switch (id) // yanderedev aa code
        {
            case 0:
                transformToUse = hostTransform;
                break;
            case 1:
                transformToUse = p1Transform;
                break;
             case 2:
                transformToUse = p2Transform;
                break;
             case 3:
                transformToUse = p3Transform;
                break;

            default:
                return;
        }

        _playersInLobby[id].transform.position = transformToUse.position;
        _playersInLobby[id].transform.rotation = transformToUse.rotation;
    }
}
