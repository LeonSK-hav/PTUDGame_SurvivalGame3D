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
    private PlayerStats playerStats;

    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
    }

    void Start()
    {
        if (playerStats != null)
        {
            // Lấy máu tối đa từ PlayerStats ban đầu
            maxHP = playerStats.currentMaxHp;
            currentHP = maxHP;
        }
        else
        {
            currentHP = maxHP;
        }
        UpdateHPUI();
    }

    // Hàm đồng bộ và cập nhật lại máu tối đa từ PlayerStats (Gọi khi trang bị đồ hoặc lên cấp)
    public void SyncMaxHealth()
    {
        if (playerStats != null)
        {
            float oldMaxHP = maxHP;
            maxHP = playerStats.currentMaxHp;

            // Nếu máu tối đa tăng/giảm, có thể cộng dồn hoặc giữ nguyên tỷ lệ % máu hiện tại
            // Ở đây ta cộng thêm phần chênh lệch máu tối đa cho currentHP (giống cơ chế hồi máu khi nâng max HP)
            float hpDifference = maxHP - oldMaxHP;
            if (hpDifference > 0)
            {
                currentHP += hpDifference; // Tăng thêm lượng máu chênh lệch khi max HP tăng
            }
            
            currentHP = Mathf.Clamp(currentHP, 0f, maxHP);
            UpdateHPUI();
        }
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
        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.ShowGameOver();
        }
        else
        {
            Debug.LogError("Không tìm thấy GameOverManager để hiển thị màn hình Game Over.", this);
        }
    }
}