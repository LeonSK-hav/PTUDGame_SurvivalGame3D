using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public CharacterController controller;
    public float walkSpeed;
    public float runSpeed;

    public bool IsWalking;
    public bool IsRunning;
    public PlayerStamina stamina;
    Vector2 moveInput;
    bool running;

    float moveSpeed;
    // Start is called before the first frame update
    void Start()
    {
        InputManager.Instance.playerInput.Player.Move.performed += HandleMoveInput;
        InputManager.Instance.playerInput.Player.Move.canceled += HandleMoveInput;
        InputManager.Instance.playerInput.Player.Sprint.performed += HandleRunningInput;
        InputManager.Instance.playerInput.Player.Sprint.canceled += HandleRunningInput;

        controller = GetComponent<CharacterController>();
        moveSpeed = walkSpeed;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetGameplayCursor();
        }
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        bool isMoving = moveInput.sqrMagnitude > 0.0001f;
        bool canRun = isMoving && running;

        if (stamina != null)
        {
            canRun = stamina.UpdateStamina(isMoving, running);
        }

        moveSpeed = canRun ? runSpeed : walkSpeed;
        IsWalking = isMoving && !canRun;
        IsRunning = canRun;
        controller.Move(moveDirection.normalized * moveSpeed * Time.deltaTime);
    }

    void HandleMoveInput(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        
    }

    void HandleRunningInput(InputAction.CallbackContext context)
    {
        running = context.performed;
    }
}
