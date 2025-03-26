using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputController : MonoBehaviour, PlayerInputActions.IGameplayActions
{
    public Action<Vector2> OnMoveInput;

    private PlayerInputActions _playerInputActions;
    private PlayerInput _playerInput;

    private const string KEYBOARD_SCHEME = "KBM";
    private const string GAMEPAD_SCHEME = "Gamepad";

    private void Awake()
    {
        _playerInputActions = new PlayerInputActions();
        _playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        _playerInputActions.Gameplay.SetCallbacks(this);
    }

    private void OnDisable()
    {
        _playerInputActions.Gameplay.RemoveCallbacks(this);
    }

    private void Start()
    {
        EnableGameplayInput();
    }

    private void EnableGameplayInput()
    {
        _playerInputActions.Gameplay.Enable();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        OnMoveInput?.Invoke(context.ReadValue<Vector2>());
    }
}
