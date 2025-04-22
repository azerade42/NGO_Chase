using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : NetworkBehaviour
{
    [SerializeField] private Canvas [] _clientUIObjects;
    [SerializeField] private Transform _clientUIParent;

    public override void OnNetworkSpawn()
    {
        if (!HasAuthority)
            return;
        
        LobbyManager.OnPlayerAddedToLobby += EnableClientUI;
    }

    public override void OnNetworkDespawn()
    {
        if (!HasAuthority)
            return;
            
        LobbyManager.OnPlayerAddedToLobby -= EnableClientUI;
    }

    private void EnableClientUI(ulong clientID) => EnableClientUIRpc(RpcTarget.Single(clientID, RpcTargetUse.Temp));

    [Rpc(SendTo.SpecifiedInParams)]
    private void EnableClientUIRpc(RpcParams rpcParams = default)
    {
        for (int i = 0; i < _clientUIObjects.Length; i++)
        {
            Instantiate(_clientUIObjects[i], _clientUIParent);
        }
    }
}
