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

        NetworkObject networkObject = other.transform.root.GetComponent<NetworkObject>();

        if (networkObject != null && networkObject.GetComponent<PlayerController>())
        {
            OnPlayerTouched?.Invoke(networkObject.OwnerClientId);
        }
    }
}
