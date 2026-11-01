using UnityEngine;
using UnityEngine.UI;
using TMPro; // Sử dụng TextMeshPro (nếu dùng Text thường thì đổi thành UnityEngine.UI.Text)
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private readonly HashSet<GameObject> openUIPanels = new HashSet<GameObject>();
    private bool cursorOverride;
    private bool hasGameplayPlayer;

    public bool IsUIOpen => openUIPanels.Count > 0;
    public bool IsGameplayInputBlocked =>
        IsUIOpen || cursorOverride || Cursor.visible || Cursor.lockState != CursorLockMode.Locked;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            hasGameplayPlayer = FindFirstObjectByType<PlayerController>() != null;
            SetCursorState(true);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null &&
            (keyboard.leftAltKey.wasPressedThisFrame || keyboard.rightAltKey.wasPressedThisFrame))
        {
            ToggleCursor();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void SetUIOpen(GameObject uiPanel, bool isOpen)
    {
        if (uiPanel == null) return;

        if (isOpen)
        {
            openUIPanels.Add(uiPanel);
            cursorOverride = false;
        }
        else
        {
            openUIPanels.Remove(uiPanel);
            if (!IsUIOpen)
            {
                cursorOverride = false;
            }
        }

        SetCursorState(IsUIOpen || cursorOverride);
    }

    public void SetGameplayCursor()
    {
        if (IsUIOpen) return;

        cursorOverride = false;
        SetCursorState(false);
    }

    private void ToggleCursor()
    {
        if (IsUIOpen) return;

        cursorOverride = !cursorOverride;
        SetCursorState(cursorOverride);
    }

    private void SetCursorState(bool isFree)
    {
        isFree |= !hasGameplayPlayer;
        Cursor.lockState = isFree ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isFree;
    }


    #region Quản lý Panel / Canvas / GameObject
    /// <summary>
    /// Bật hoặc tắt một GameObject (Canvas, Panel, v.v.)
    /// </summary>
    public void ToggleGameObject(GameObject targetObj)
    {
        if (targetObj != null)
        {
            bool isOpening = !targetObj.activeSelf;
            targetObj.SetActive(isOpening);
            SetUIOpen(targetObj, isOpening);
        }
    }

    
    /// <summary>
    /// Kích hoạt (Bật) một GameObject
    /// </summary>
    public void ShowGameObject(GameObject targetObj)
    {
        if (targetObj != null) {
            targetObj.SetActive(true);
            SetUIOpen(targetObj, true);
        }

    }

    /// <summary>
    /// Ẩn (Tắt) một GameObject
    /// </summary>
    public void HideGameObject(GameObject targetObj)
    {
        if (targetObj != null)
        {
            targetObj.SetActive(false);
            SetUIOpen(targetObj, false);
        }
    }

    /// <summary>
    /// Ẩn hiện Canvas bằng cách chỉnh CanvasGroup (giữ lại trạng thái để làm hiệu ứng mờ dần nếu muốn)
    /// </summary>
    public void SetCanvasGroupState(CanvasGroup canvasGroup, bool isOpen, float alphaOpen = 1f, float alphaClose = 0f)
    {
        if (canvasGroup == null) return;

        canvasGroup.alpha = isOpen ? alphaOpen : alphaClose;
        canvasGroup.interactable = isOpen;
        canvasGroup.blocksRaycasts = isOpen;
        SetUIOpen(canvasGroup.gameObject, isOpen);
    }
    #endregion

    #region Quản lý Text
    /// <summary>
    /// Đổi nội dung TextMeshProUGUI
    /// </summary>
    public void SetText(TextMeshProUGUI textComponent, string newText)
    {
        if (textComponent != null) textComponent.text = newText;
    }

    /// <summary>
    /// Đổi nội dung Text thường (Legacy UI)
    /// </summary>
    public void SetLegacyText(Text textComponent, string newText)
    {
        if (textComponent != null) textComponent.text = newText;
    }
    #endregion

    #region Quản lý Button
    /// <summary>
    /// Bật/Tắt trạng thái tương tác của nút (Interactable)
    /// </summary>
    public void SetButtonInteractable(Button button, bool isInteractable)
    {
        if (button != null) button.interactable = isInteractable;
    }
    #endregion
    
}