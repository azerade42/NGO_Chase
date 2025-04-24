using System;
using UnityEngine;
using Unity.Netcode;

public class NetworkedHitbox : NetworkBehaviour
{
    public event Action<ulong> OnPlayerTouched;

    private void OnTriggerEnter(Collider other)
    {
        // Only handle collisions on the server
        if (!HasAuthority)
            return;

        NetworkObject networkObject = other.GetComponentInParent<NetworkObject>();

        if (networkObject != null && TryGetComponent(out PlayerController pc))
        {
            OnPlayerTouched?.Invoke(networkObject.OwnerClientId);
        }
    }
}
