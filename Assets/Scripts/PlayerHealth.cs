using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    [Header("UI Components")]
    public Slider hpSlider;          // Kéo Slider HP_Bar vào đây
    public TextMeshProUGUI hpText;   // (Tùy chọn) Text hiển thị số máu dạng chữ

    [Header("Health Stats")]
    public float maxHP = 100f;
    private float currentHP;

    void Start()
    {
        currentHP = maxHP;
        UpdateHPUI();
    }

    // Hàm nhận sát thương (Gọi hàm này khi bị zombie đánh trúng)
    public void TakeDamage(float damageAmount)
    {
        currentHP -= damageAmount;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP); // Giới hạn máu từ 0 đến max

        UpdateHPUI();

        if (currentHP <= 0)
        {
            Die();
        }
    }

    // Hàm hồi máu
    public void Heal(float healAmount)
    {
        currentHP += healAmount;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        UpdateHPUI();
    }

    // Cập nhật hiển thị lên giao diện
    private void UpdateHPUI()
    {
        if (hpSlider != null)
        {
            hpSlider.value = currentHP; // Thanh slider tự động co giãn theo giá trị
        }

        if (hpText != null)
        {
            hpText.text = Mathf.Ceil(currentHP) + " / " + maxHP;
        }
    }

    private void Die()
    {
        Debug.Log("Nhân vật đã ngã xuống trong vòng vây của zombie!");
        // Thêm logic Game Over ở đây
    }
}