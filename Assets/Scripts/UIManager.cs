using UnityEngine;
using UnityEngine.UI;
using TMPro; // Sử dụng TextMeshPro (nếu dùng Text thường thì đổi thành UnityEngine.UI.Text)

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }


    #region Quản lý Panel / Canvas / GameObject
    /// <summary>
    /// Bật hoặc tắt một GameObject (Canvas, Panel, v.v.)
    /// </summary>
    public void ToggleGameObject(GameObject targetObj)
    {
        if (targetObj != null)
        {
            targetObj.SetActive(!targetObj.activeSelf);
        }
    }

    
    /// <summary>
    /// Kích hoạt (Bật) một GameObject
    /// </summary>
    public void ShowGameObject(GameObject targetObj)
    {
        if (targetObj != null) {
            targetObj.SetActive(true);
        }

    }

    /// <summary>
    /// Ẩn (Tắt) một GameObject
    /// </summary>
    public void HideGameObject(GameObject targetObj)
    {
        if (targetObj != null) targetObj.SetActive(false);
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