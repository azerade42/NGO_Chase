using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine;
using System;
using Unity.Netcode.Components;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(PlayerInputController), typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    public static Action OnTaggedHost;

    private CharacterController _controller;
    private PlayerInputController _playerInput;
    private Vector2 _moveDirection;
    
    float verticalVelocity = 0;
    bool mouseLocked = false;
    bool _movementDisabled;
    bool _lockControls;

    [SerializeField] private NetworkedHitbox _hitbox;
    [SerializeField] private CinemachineCamera _freeCam;
    [SerializeField] private Renderer _bodyRenderer;
    [SerializeField] private Animator _animator;
    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _lookSpeed;
    [SerializeField] private AnimationCurve _diveCurve;

    private readonly int _moveSpeedParameter = Animator.StringToHash("Speed");
    private readonly int _diveParameter = Animator.StringToHash("Dive");

    private NetworkVariable<Color> _bodyColor = new(
        Color.blue,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<Vector3> _startPos = new(
        default,
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
        _playerInput.OnDiveInput += Dive;
        _hitbox.OnPlayerTouched += NotifyAnotherPlayerTouchedRpc;
        RoundManager.OnHidePhaseStarted += ToggleClientMovementOffRpc;
        RoundManager.OnChasePhaseStarted += ToggleMovementOnRpc;
    }

    private void OnDisable()
    {
        _playerInput.OnMoveInput -= UpdateMoveDirection;
        _playerInput.OnDiveInput -= Dive;
        _hitbox.OnPlayerTouched -= NotifyAnotherPlayerTouchedRpc;
        RoundManager.OnChasePhaseStarted -= ToggleClientMovementOffRpc;
        RoundManager.OnChasePhaseStarted -= ToggleMovementOnRpc;
    }

    // Runs before start because players are spawned dynamically
    public override void OnNetworkSpawn()
    {
        if (HasAuthority) // Runs only on server
        {
            if (IsLocalPlayer) // Runs only for host on server
            {
                _bodyColor.Value = Color.red;
                _moveSpeed *= 1.2f;
            }
            else
            {
                _bodyColor.Value = UnityEngine.Random.ColorHSV(0.3f, 0.7f, 1, 1, 0.5f, 0.5f);
                Vector2 randomPos = UnityEngine.Random.insideUnitCircle.normalized * 5f;
                _startPos.Value = new Vector3(randomPos.x, transform.position.y, randomPos.y);
            }
        }

        if (IsOwner) // Runs only on the client that controls this object
        {
            _freeCam.Target.TrackingTarget = transform;
            ToggleMouseLock();
        }
        
        if (!IsOwner) // Runs only on clients that don't control this object
        {
            _playerInput.DeactivateInput();
            _freeCam.gameObject.SetActive(false);
        }

        // Runs on server and all clients
        _bodyRenderer.material.color = _bodyColor.Value;
        _bodyRenderer.material.SetColor("_EmissionColor", _bodyColor.Value);
        _controller.Move(_startPos.Value);
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            ToggleMouseLock();
        }

        if (!_movementDisabled)
            Move();
    }

    private void Dive()
    {        
        _lockControls = true;
        _animator.SetTrigger(_diveParameter);

        StartCoroutine(WaitForDive());
    }

    private IEnumerator WaitForDive()
    {
        yield return new WaitForEndOfFrame();
        _animator.ResetTrigger(_diveParameter);

        float startTime = Time.time;
        float endTime = startTime + 2f;
        Vector3 flatForward = Vector3.ProjectOnPlane(_freeCam.transform.forward, Vector3.up).normalized;

        // Raycast to get slope
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit slopeHit, 1.5f))
        {
            // Align movement to slope
            flatForward = Vector3.ProjectOnPlane(flatForward, slopeHit.normal).normalized;
        }
        while (Time.time < endTime)
        {
            float easing = _diveCurve.Evaluate((Time.time - startTime) / (endTime - startTime));
            _controller.Move(flatForward * 4.5f * easing * Time.deltaTime);
            yield return null;
        }
        
        yield return new WaitForSeconds(2f);

        _lockControls = false;
    }

    private void Move()
    {
        if (!_lockControls)
        {
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
            _animator.SetFloat(_moveSpeedParameter, movement.magnitude * _moveSpeed);

            Vector3 lookDirection = new Vector3(movement.x, 0, movement.z);

            if (movement.sqrMagnitude > Mathf.Epsilon * Mathf.Epsilon)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _lookSpeed * Time.deltaTime);
            }
        }

        if (_controller.isGrounded)
        {
            verticalVelocity = -1f;
        }
        else
        {
            verticalVelocity += -9.81f * Time.deltaTime;
        }

        Vector3 gravityMove = Vector3.up * verticalVelocity;
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

    [Rpc(SendTo.ClientsAndHost)]
    public void TeleportRpc(Vector3 position)
    {
        transform.position = position;
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void RotateRpc(Quaternion rotation)
    {
        transform.rotation = rotation;
    }
    
    [Rpc(SendTo.ClientsAndHost)]
    private void ToggleClientMovementOffRpc()
    {
        if (HasAuthority && IsLocalPlayer)
            return;
        
        _movementDisabled = true;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ToggleMovementOnRpc() => _movementDisabled = false;

    [Rpc(SendTo.Server)]
    private void NotifyAnotherPlayerTouchedRpc(ulong clientID)
    {
        if (clientID == 0)
            OnTaggedHost?.Invoke();
    }
}
