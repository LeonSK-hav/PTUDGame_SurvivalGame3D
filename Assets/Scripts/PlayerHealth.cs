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
    private bool isDead;

    private void Awake()
    {
        currentHP = Mathf.Max(0f, maxHP);
    }

    void Start()
    {
        UpdateHPUI();
    }

    public void TakeDamage(int amount)
    {
        TakeDamage((float)amount);
    }

    // Hàm nhận sát thương (Gọi hàm này khi bị zombie đánh trúng)
    public void TakeDamage(float damageAmount)
    {
        if (isDead || damageAmount <= 0f) return;

        currentHP = Mathf.Max(0f, currentHP - damageAmount);

        UpdateHPUI();

        if (currentHP <= 0f)
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
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;
        }

        if (hpText != null)
        {
            hpText.text = Mathf.Ceil(currentHP) + " / " + maxHP;
        }
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log("Nhân vật đã ngã xuống trong vòng vây của zombie!");
        // Thêm logic Game Over ở đây
    }
}