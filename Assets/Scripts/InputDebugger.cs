using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputDebugger : MonoBehaviour
{
    public Vector2 moveInput;
    public bool running;
    PlayerInputs playerInput;

    private void Awake()
    {
        playerInput = new();
        playerInput.Enable();
    }

    private void OnDisable()
    {
        playerInput.Disable();
    }
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        playerInput.Player.Move.performed += OnMove;
        playerInput.Player.Move.canceled += OnMove;
        playerInput.Player.Sprint.performed += OnRun;
        playerInput.Player.Sprint.canceled += OnRun;
    }

    void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    void OnRun(InputAction.CallbackContext context)
    {
        running = context.performed;
    }
}
