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

    public float CurrentHealth => currentHP;
    public float MaxHealth => maxHP;
    public bool CanHeal => !isDead && currentHP < maxHP;

    private void Awake()
    {
        maxHP = Mathf.Max(0f, maxHP);
        currentHP = maxHP;
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
        if (isDead || healAmount <= 0f) return;

        currentHP = Mathf.Min(currentHP + healAmount, maxHP);
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