using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [HideInInspector] public Transform ObjToFollow;
    [SerializeField] private Vector3 _lookOffset;

    private void LateUpdate()
    {
        if (!ObjToFollow)
            return;

        transform.position = ObjToFollow.position + ObjToFollow.rotation * _lookOffset;
        transform.LookAt(ObjToFollow);
    }
}
