using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerInputController), typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    private CharacterController _controller;
    private PlayerInputController _playerInput;
    private Vector2 _moveDirection;
    private float _rotationDelta;

    [SerializeField] private GameObject _playerCameraPrefab;
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

        Quaternion rotationChange = Quaternion.Euler(0, _rotationDelta, 0);
        Quaternion newRotation = transform.rotation * rotationChange;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, newRotation, Time.deltaTime * _lookSpeed);

        Vector3 movement = new Vector3(_moveDirection.x, 0, _moveDirection.y);
        Vector3 movementRotated = transform.right * movement.x + transform.forward * movement.z;
        _controller.Move(movementRotated * _moveSpeed * Time.deltaTime);
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
        GameObject playerCamera = Instantiate(_playerCameraPrefab);
        if (playerCamera.TryGetComponent(out Camera cam))
        {
            if (playerCamera.TryGetComponent(out CameraFollow follow))
            {
                follow.ObjToFollow = transform;
                Camera.main.gameObject.SetActive(false);
                cam.gameObject.SetActive(true);
                cam.gameObject.AddComponent<AudioListener>();
            }
        }
    }
}
