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
    public AudioClip footstepSound;
    [Header("Footstep Settings")]
    public float walkStepInterval = 0.5f;
    public float runStepInterval = 0.3f;
    private float stepTimer;
    
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
/*
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
         if (AudioManager.Instance)
        {
            AudioManager.Instance.PlayEffectSound(footstepSound);
        }
        controller.Move(moveDirection.normalized * moveSpeed * Time.deltaTime);
    }
    */
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

        // Xử lý phát âm thanh bước chân có khoảng nghỉ
        if (isMoving)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                if (AudioManager.Instance != null && footstepSound != null)
                {
                    AudioManager.Instance.PlayEffectSound(footstepSound);
                }

                stepTimer = canRun ? runStepInterval : walkStepInterval;
            }
        }
        else
        {
                // Khi nhân vật dừng di chuyển, lập tức dừng ngay âm thanh bước chân đang chạy dở
                if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopEffectSound();
            }
            stepTimer = 0f;
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
