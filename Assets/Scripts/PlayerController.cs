using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Quản lý di chuyển, chạy nhanh (sprint), trọng lực (gravity), nhảy (jump)
/// và âm thanh bước chân của người chơi.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Controller & Movement Speeds")]
    public CharacterController controller;
    public float walkSpeed = 5f;
    public float runSpeed = 8f;

    [Header("Movement States")]
    public bool IsWalking;
    public bool IsRunning;
    public PlayerStamina stamina;

    [Header("Gravity & Jump Settings")]
    [Tooltip("Gia tốc trọng lực (m/s^2), giá trị âm. Mặc định -15 để tạo cảm giác rơi nhanh, đầm và dứt khoát.")]
    public float gravity = -15f;

    [Tooltip("Độ cao cú nhảy (mét).")]
    [Min(0.1f)] public float jumpHeight = 1.2f;

    [Tooltip("Lực hút nhẹ xuống đất khi đang đứng để giữ player bám sát địa hình dốc/gồ ghề không bị nảy.")]
    public float groundedGravity = -2f;

    [Tooltip("Tốc độ rơi tối đa (terminal velocity)")]
    public float terminalVelocity = -30f;

    [Tooltip("Thể lực tiêu hao cho mỗi cú nhảy (0 = không tốn)")]
    public float jumpStaminaCost = 0f;

    [Header("Audio")]
    public AudioClip footstepSound;
    public AudioClip jumpSound;
    public AudioClip landSound;

    [Header("Footstep Settings")]
    public float walkStepInterval = 0.5f;
    public float runStepInterval = 0.3f;
    private float stepTimer;

    // Trạng thái nội bộ
    private Vector2 moveInput;
    private bool running;
    private float moveSpeed;
    private float verticalVelocity;
    private bool jumpRequested;
    private bool isGrounded;
    private bool wasGrounded;
    private bool isSubscribed;

    public bool IsGrounded => isGrounded;
    public float VerticalVelocity => verticalVelocity;

    private void Awake()
    {
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }

        moveSpeed = walkSpeed;
    }

    private void Start()
    {
        SubscribeInputs();

        if (stamina == null)
        {
            stamina = GetComponent<PlayerStamina>();
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetGameplayCursor();
        }
    }

    private void OnEnable()
    {
        SubscribeInputs();
    }

    private void OnDisable()
    {
        UnsubscribeInputs();
        moveInput = Vector2.zero;
        running = false;
        jumpRequested = false;
    }

    private void OnDestroy()
    {
        UnsubscribeInputs();
    }

    private void SubscribeInputs()
    {
        if (isSubscribed) return;
        if (InputManager.Instance == null || InputManager.Instance.playerInput == null) return;

        var playerActions = InputManager.Instance.playerInput.Player;
        playerActions.Move.performed += HandleMoveInput;
        playerActions.Move.canceled += HandleMoveInput;
        playerActions.Sprint.performed += HandleRunningInput;
        playerActions.Sprint.canceled += HandleRunningInput;
        playerActions.Jump.performed += HandleJumpInput;

        isSubscribed = true;
    }

    private void UnsubscribeInputs()
    {
        if (!isSubscribed) return;
        if (InputManager.Instance == null || InputManager.Instance.playerInput == null)
        {
            isSubscribed = false;
            return;
        }

        var playerActions = InputManager.Instance.playerInput.Player;
        playerActions.Move.performed -= HandleMoveInput;
        playerActions.Move.canceled -= HandleMoveInput;
        playerActions.Sprint.performed -= HandleRunningInput;
        playerActions.Sprint.canceled -= HandleRunningInput;
        playerActions.Jump.performed -= HandleJumpInput;

        isSubscribed = false;
    }

    private void Update()
    {
        if (!isSubscribed)
        {
            SubscribeInputs();
        }

        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
            if (controller == null) return;
        }

        // 1. Kiểm tra trạng thái UI chặn gameplay (khi mở Inventory, Shop, Pause menu...)
        bool gameplayInputBlocked =
            (UIManager.Instance != null && UIManager.Instance.IsGameplayInputBlocked) ||
            Cursor.visible || CursorLockMode.Locked != Cursor.lockState;

        Vector2 effectiveMoveInput = gameplayInputBlocked ? Vector2.zero : moveInput;
        bool effectiveRunning = !gameplayInputBlocked && running;

        // Bổ sung kiểm tra phím Space trực tiếp qua Input System để đảm bảo không bị trượt frame
        if (!gameplayInputBlocked && !jumpRequested)
        {
            if (InputManager.Instance != null && InputManager.Instance.playerInput != null)
            {
                if (InputManager.Instance.playerInput.Player.Jump.WasPressedThisFrame())
                {
                    jumpRequested = true;
                }
            }
            else if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                jumpRequested = true;
            }
        }

        // 2. Kiểm tra trạng thái tiếp đất
        isGrounded = controller.isGrounded;

        // Âm thanh tiếp đất (Landing)
        if (!wasGrounded && isGrounded)
        {
            if (verticalVelocity < -6f && AudioManager.Instance != null && landSound != null)
            {
                AudioManager.Instance.PlayEffectSound(landSound);
            }
        }

        // 3. Xử lý trọng lực và Nhảy (Jumping)
        if (isGrounded && verticalVelocity < 0f)
        {
            // Lực hút bám đất khi di chuyển trên địa hình gồ ghề/dốc
            verticalVelocity = groundedGravity;
        }

        // Thực hiện nhảy khi có yêu cầu và đang đứng trên đất
        if (jumpRequested)
        {
            jumpRequested = false;

            if (isGrounded && !gameplayInputBlocked)
            {
                bool canJump = true;
                if (stamina != null && jumpStaminaCost > 0f)
                {
                    if (stamina.currentStamina < jumpStaminaCost)
                    {
                        canJump = false;
                    }
                    else
                    {
                        stamina.currentStamina = Mathf.Max(0f, stamina.currentStamina - jumpStaminaCost);
                    }
                }

                if (canJump)
                {
                    // Công thức chuẩn vật lý: v = sqrt(2 * h * g)
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

                    if (AudioManager.Instance != null && jumpSound != null)
                    {
                        AudioManager.Instance.PlayEffectSound(jumpSound);
                    }
                }
            }
        }

        // Tích lũy gia tốc trọng lực kéo xuống theo thời gian
        verticalVelocity += gravity * Time.deltaTime;
        if (verticalVelocity < terminalVelocity)
        {
            verticalVelocity = terminalVelocity;
        }

        // 4. Xử lý di chuyển ngang (Horizontal Movement)
        Vector3 moveDirection = transform.right * effectiveMoveInput.x + transform.forward * effectiveMoveInput.y;
        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        bool isMoving = effectiveMoveInput.sqrMagnitude > 0.0001f;
        bool canRun = isMoving && effectiveRunning && isGrounded;

        if (stamina != null && isGrounded)
        {
            canRun = stamina.UpdateStamina(isMoving, effectiveRunning);
        }

        moveSpeed = canRun ? runSpeed : walkSpeed;
        IsWalking = isMoving && !canRun && isGrounded;
        IsRunning = canRun && isGrounded;

        // 5. Phát âm thanh bước chân (chỉ khi đang di chuyển trên mặt đất)
        if (isMoving && isGrounded)
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
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopEffectSound();
            }
            stepTimer = 0f;
        }

        // 6. Tổng hợp vector vận tốc và dịch chuyển bằng CharacterController
        Vector3 finalVelocity = moveDirection * moveSpeed;
        finalVelocity.y = verticalVelocity;

        controller.Move(finalVelocity * Time.deltaTime);

        wasGrounded = isGrounded;
    }

    private void HandleMoveInput(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void HandleRunningInput(InputAction.CallbackContext context)
    {
        running = context.performed;
    }

    private void HandleJumpInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            jumpRequested = true;
        }
    }
}
