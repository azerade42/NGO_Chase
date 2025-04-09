using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine;

[RequireComponent(typeof(PlayerInputController), typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    
    private CharacterController _controller;
    private PlayerInputController _playerInput;
    private Vector2 _moveDirection;
    private float _rotationDelta;
    
    float verticalVelocity = 0;

    [SerializeField] private CinemachineCamera freeCam;
    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _lookSpeed;
    bool mouseLocked = false;

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
        _playerInput.OnLookInput += UpdateLookDirection;
    }

    private void OnDisable()
    {
        _playerInput.OnMoveInput -= UpdateMoveDirection;
        _playerInput.OnLookInput -= UpdateLookDirection;
    }

    // used instead of start
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _bodyColor.Value = Random.ColorHSV(0, 1, 1, 1, 1, 1);
        }

        transform.GetChild(0).GetComponent<Renderer>().material.color = _bodyColor.Value;

        if (!IsOwner)
            return;
        
        InitializeCamera();
        ToggleMouseLock();


    }

    // public override void OnNetworkDespawn()
    // {
    //     if (!IsServer)
    //         _bodyColor.OnValueChanged -= OnBodyColorChanged;

        
    // }

    // private void OnBodyColorChanged(Color oldValue, Color newValue)
    // {
    //     _bodyColor
    // }

    private void Start()
    {
        
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            ToggleMouseLock();
        }

        Vector3 flatForward = Vector3.ProjectOnPlane(freeCam.transform.forward, Vector3.up).normalized;
        Vector3 flatRight = Vector3.ProjectOnPlane(freeCam.transform.right, Vector3.up).normalized;

        Vector3 movement = _moveDirection.x * flatRight + _moveDirection.y * flatForward;

        // Raycast to get slope
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit slopeHit, 1.5f))
        {
            // Align movement to slope
            movement = Vector3.ProjectOnPlane(movement, slopeHit.normal).normalized;
        }

        _controller.Move(movement * _moveSpeed * Time.deltaTime);

        // Vector3 movement = (_moveDirection.x * flatRight + _moveDirection.y * flatForward).normalized;
        // _controller.Move(_moveSpeed * Time.deltaTime * movement);

        Vector3 lookDirection = new Vector3(movement.x, 0, movement.z);

        if (movement.sqrMagnitude > Mathf.Epsilon * Mathf.Epsilon)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _lookSpeed * Time.deltaTime);
        }

        if (_controller.isGrounded)
        {
            verticalVelocity = -1f; // slight downward to stay grounded
        }
        else
        {
            verticalVelocity += -9.81f * Time.deltaTime;
        }

        Vector3 gravityMove = new Vector3(0, verticalVelocity, 0);
        _controller.Move(gravityMove * Time.deltaTime);


        // Quaternion rotationChange = Quaternion.Euler(0, _rotationDelta, 0);
        // Quaternion newRotation = transform.rotation * rotationChange;
        // transform.rotation = Quaternion.RotateTowards(transform.rotation, newRotation, Time.deltaTime * _lookSpeed);

        // Vector3 movement = new Vector3(_moveDirection.x, 0, _moveDirection.y);
        // Vector3 movementRotated = transform.right * movement.x + transform.forward * movement.z;
        // _controller.Move(movementRotated * _moveSpeed * Time.deltaTime);
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

    private void UpdateLookDirection(float delta) => _rotationDelta = delta;

    private void InitializeCamera()
    {
        freeCam.Target.TrackingTarget = transform;
        // GameObject playerCamera = Instantiate(_playerCameraPrefab);
        // if (playerCamera.TryGetComponent(out Camera cam))
        // {
        //     if (playerCamera.TryGetComponent(out CameraFollow follow))
        //     {
        //         follow.ObjToFollow = transform;
        //         Camera.main.gameObject.SetActive(false);
        //         cam.gameObject.SetActive(true);
        //         cam.gameObject.AddComponent<AudioListener>();
        //     }
        // }
    }
}
