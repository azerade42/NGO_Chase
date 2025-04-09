using UnityEngine;
using Unity.Netcode;

public class CameraController : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
            return;

        
    }
}
