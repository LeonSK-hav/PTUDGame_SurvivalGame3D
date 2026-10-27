using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class ButtonTextColorChanger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private TextMeshProUGUI buttonText;
      

    // Thiết lập màu chữ cho từng trạng thái
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(1f, 0.9f, 0.6f); // Màu vàng sáng khi rê chuột
    public Color pressedColor = new Color(0.9f, 0.7f, 0.3f); // Màu khi bấm

    void Start()
    {
        // Tự động tìm component TextMeshPro bên trong nút
        buttonText = GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.color = normalColor;
        }
    }
       
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (buttonText != null) buttonText.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (buttonText != null) buttonText.color = normalColor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (buttonText != null) buttonText.color = pressedColor;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (buttonText != null) buttonText.color = hoverColor;
    }
}