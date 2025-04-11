using System;
using UnityEngine;
using Unity.Netcode;

public class NetworkedHitbox : NetworkBehaviour
{
    public event Action<ulong> OnPlayerTouched;
    private ulong clientID;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
            return;

        clientID = GetComponentInParent<NetworkObject>().OwnerClientId;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only handle collisions on the server
        if (!HasAuthority)
            return;

        var networkObject = other.GetComponentInParent<NetworkObject>();

        if (networkObject != null)
        {
            ulong hitClientId = networkObject.OwnerClientId;

            // if two players hit each other and neither are the host
            if (hitClientId != 0 && clientID != 0)
                return;

            // Notify other systems on the server
            OnPlayerTouched?.Invoke(clientID);

            NotifyHitClientRpc(RpcTarget.Single(hitClientId, RpcTargetUse.Temp));
        }
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void NotifyHitClientRpc(RpcParams rpcParams = default)
    {
        // client-only stuff
    }
}
