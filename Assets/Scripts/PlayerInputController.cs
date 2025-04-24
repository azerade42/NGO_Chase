using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class PlayerInputController : MonoBehaviour, PlayerInputActions.IGameplayActions
{
    public Action<Vector2> OnMoveInput;
    public Action<float> OnLookInput;
    public Action OnDiveInput;

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
        _playerInputActions.Gameplay.Enable();
        _playerInput.ActivateInput();
    }

    private void OnDisable()
    {
        DeactivateInput();
    }

    public void DeactivateInput()
    {
        _playerInputActions.Gameplay.RemoveCallbacks(this);
        _playerInputActions.Gameplay.Disable();
        _playerInput.DeactivateInput();
    }
    

    public void OnMove(InputAction.CallbackContext context)
    {
        OnMoveInput?.Invoke(context.ReadValue<Vector2>());
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        OnLookInput?.Invoke(context.ReadValue<float>());
    }

    public void OnDive(InputAction.CallbackContext context)
    {
        OnDiveInput?.Invoke();
    }
}
