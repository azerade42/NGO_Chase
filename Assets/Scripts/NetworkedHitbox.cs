using System;
using UnityEngine;
using Unity.Netcode;

public class NetworkedHitbox : NetworkBehaviour
{
    public event Action OnPlayerTouched;

    private void OnTriggerEnter(Collider other)
    {
        // Only handle collisions on the server
        if (!HasAuthority)
            return;

        PlayerController networkObject = other.GetComponentInParent<PlayerController>();

        if (networkObject != null)
        {
            OnPlayerTouched?.Invoke();
        }
    }
}
