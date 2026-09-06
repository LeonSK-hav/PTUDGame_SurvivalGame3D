using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public CharacterController controller;
    public float walkSpeed;
    public float runSpeed;
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
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        if (running == true)
        {
            moveSpeed = runSpeed;
        }
        else
        {
            moveSpeed = walkSpeed;
        }
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
