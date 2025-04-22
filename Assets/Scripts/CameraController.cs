using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine;

public class LobbyCameraController : NetworkBehaviour
{
    CinemachineOrbitalFollow follow;
    [SerializeField] private float orbitSpeed;
    private void Start()
    {
        follow = GetComponent<CinemachineOrbitalFollow>();
    }

    public override void OnNetworkSpawn()
    {
        LobbyManager.OnPlayerAddedToLobby += RotatePlayerToCamera;
    }

    public override void OnNetworkDespawn()
    {
        LobbyManager.OnPlayerAddedToLobby -= RotatePlayerToCamera;
    }

    private void Update()
    {
        follow.HorizontalAxis.Value += orbitSpeed * Time.deltaTime;

        if (follow.HorizontalAxis.Value >= 180f)
            follow.HorizontalAxis.Value = -180f;
    }

    public void RotatePlayerToCamera(ulong clientID)
    {
        float yRot = transform.rotation.eulerAngles.y;
        Quaternion newRot = Quaternion.Euler(0, yRot, 0);
        PlayerController pc = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientID).GetComponent<PlayerController>();
        pc.RotateRpc(newRot);
    }
}
