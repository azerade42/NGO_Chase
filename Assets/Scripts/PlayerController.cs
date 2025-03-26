using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerInputController), typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    CharacterController _controller;
    PlayerInputController _playerInput;

    private Vector2 _moveDirection;
    [SerializeField] private float _moveSpeed;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _playerInput = GetComponent<PlayerInputController>();
    }
    private void OnEnable()
    {
        _playerInput.OnMoveInput += UpdateMoveDirection;
    }

    private void OnDisable()
    {
        _playerInput.OnMoveInput -= UpdateMoveDirection;
    }

    private void Update()
    {
        if (!IsOwner || !Application.isFocused)
            return;

        Vector3 movement = new Vector3(_moveDirection.x, 0, _moveDirection.y);
        _controller.Move(movement * _moveSpeed * Time.deltaTime);
    }

    private void UpdateMoveDirection(Vector2 direction)
    {
        _moveDirection = direction.normalized;
    }


}
