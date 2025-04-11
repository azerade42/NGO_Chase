using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine;
using System;

[RequireComponent(typeof(PlayerInputController), typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    public static Action<Transform> OnSpawnedServer;
    public static Action<ulong> OnTouchedAnotherPlayer;

    private CharacterController _controller;
    private PlayerInputController _playerInput;
    private Vector2 _moveDirection;
    
    float verticalVelocity = 0;
    bool mouseLocked = false;
    bool _movementDisabled;

    [SerializeField] private NetworkedHitbox _hitbox;
    [SerializeField] private CinemachineCamera _freeCam;
    [SerializeField] private Renderer _bodyRenderer;
    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _lookSpeed;

    private NetworkVariable<Color> _bodyColor = new(
        Color.blue,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _playerInput = GetComponent<PlayerInputController>();
    }
    private void OnEnable()
    {
        _playerInput.OnMoveInput += UpdateMoveDirection;
        _hitbox.OnPlayerTouched += NotifyAnotherPlayerTouchedRpc;
        RoundManager.OnHidePhaseStarted += ToggleMovementOff;
        RoundManager.OnChasePhaseStarted += ToggleMovementOn;
    }

    private void OnDisable()
    {
        _playerInput.OnMoveInput -= UpdateMoveDirection;
        _hitbox.OnPlayerTouched -= NotifyAnotherPlayerTouchedRpc;
        RoundManager.OnChasePhaseStarted -= ToggleMovementOff;
        RoundManager.OnChasePhaseStarted -= ToggleMovementOn;
    }

    // Runs before start because players are spawned dynamically
    public override void OnNetworkSpawn()
    {
        if (HasAuthority && IsLocalPlayer)
        {
            _bodyColor.Value = Color.red;
        }
        else if (HasAuthority)
        {
            _bodyColor.Value = UnityEngine.Random.ColorHSV(0.3f, 0.7f, 1, 1, 1, 1);
            Vector2 randomPos = UnityEngine.Random.insideUnitCircle.normalized * 5f;
            Vector3 movement = new Vector3(randomPos.x, transform.position.y, randomPos.y);
            _controller.Move(movement);
        }

        _bodyRenderer.material.color = _bodyColor.Value;

        if (!IsOwner)
        {
            _freeCam.gameObject.SetActive(false);
            return;
        }

        _freeCam.Target.TrackingTarget = transform;
        ToggleMouseLock();
    }

    private void Start()
    {
        if (HasAuthority)
            OnSpawnedServer?.Invoke(transform);
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            ToggleMouseLock();
        }

        if (_movementDisabled)
            return;

        Vector3 flatForward = Vector3.ProjectOnPlane(_freeCam.transform.forward, Vector3.up).normalized;
        Vector3 flatRight = Vector3.ProjectOnPlane(_freeCam.transform.right, Vector3.up).normalized;

        Vector3 movement = _moveDirection.x * flatRight + _moveDirection.y * flatForward;

        // Raycast to get slope
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit slopeHit, 1.5f))
        {
            // Align movement to slope
            movement = Vector3.ProjectOnPlane(movement, slopeHit.normal).normalized;
        }

        _controller.Move(movement * _moveSpeed * Time.deltaTime);

        Vector3 lookDirection = new Vector3(movement.x, 0, movement.z);

        if (movement.sqrMagnitude > Mathf.Epsilon * Mathf.Epsilon)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _lookSpeed * Time.deltaTime);
        }

        if (_controller.isGrounded)
        {
            verticalVelocity = -1f;
        }
        else
        {
            verticalVelocity += -9.81f * Time.deltaTime;
        }

        Vector3 gravityMove = new Vector3(0, verticalVelocity, 0);
        _controller.Move(gravityMove * Time.deltaTime);
    }

    private void ToggleMouseLock()
    {
        if (!mouseLocked)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            mouseLocked = true;
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            mouseLocked = false;
        }
    }

    private void UpdateMoveDirection(Vector2 direction) => _moveDirection = direction.normalized;
    private void ToggleMovementOff()
    {
        if (HasAuthority && IsLocalPlayer)
            return;
        
        _movementDisabled = true;
    }

    private void ToggleMovementOn()
    {
        _movementDisabled = false;
    }

    [Rpc(SendTo.Server)]
    private void NotifyAnotherPlayerTouchedRpc(ulong clientID)
    {
        OnTouchedAnotherPlayer?.Invoke(clientID);
    }
}
