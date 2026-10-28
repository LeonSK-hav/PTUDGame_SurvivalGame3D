using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIEffectPulsing : MonoBehaviour
{
    [Header("Glow Settings")]
    public Image targetImage;         // Kéo Image nền của nút vào đây
    public Color normalColor = Color.white;
    public Color glowColor = new Color(1f, 0.85f, 0.2f); // Màu vàng sáng phát quang
    public float speed = 5f;          // Tốc độ nhấp nháy

    [Header("Condition: Kiểm tra đối tượng")]
    public GameObject targetObject;   // Kéo đối tượng cần kiểm tra vào đây (ví dụ: item trong kho, hoặc một object quản lý)

    [Header("Scale Pulse (Phóng to thu nhỏ nhẹ)")]
    public bool enableScalePulse = true;
    public float minScale = 0.95f;
    public float maxScale = 1.05f;

    private Vector3 originalScale;

    void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();
        
        originalScale = transform.localScale;
    }

    void OnEnable()
    {
        /*
        ResetEffect();*/
    }

    void Update()
    {
        // Kiểm tra điều kiện: Nếu có gán targetObject mà object đó đã bị mất (null) hoặc bị tắt (inactive)
        if (targetObject == null || !targetObject.activeInHierarchy)
        {
            ResetEffect(); // Dừng hiệu ứng, trả về màu và kích thước gốc
            return;        // Thoát khỏi hàm, không chạy tiếp phần nhấp nháy
        }

        // --- NẾU ĐỐI TƯỢNG VẪN CÒN, CHẠY HIỆU ỨNG TỎA SÁNG ---
        float lerp = Mathf.PingPong(Time.unscaledTime * speed, 1f);

        // 1. Hiệu ứng đổi màu sáng tối liên tục
        if (targetImage != null)
        {
            targetImage.color = Color.Lerp(normalColor, glowColor, lerp);
        }

        // 2. Hiệu ứng co giãn nhẹ
        /*
        if (enableScalePulse)
        {
            float scale = Mathf.Lerp(minScale, maxScale, lerp);
            transform.localScale = originalScale * scale;
        }*/
    }

    // Hàm đưa nút trở lại trạng thái bình thường
    void ResetEffect()
    {
        if (targetImage != null)
            targetImage.color = normalColor;
        
        transform.localScale = originalScale;
    }
}