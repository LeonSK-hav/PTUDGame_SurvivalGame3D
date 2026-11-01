using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    [Header("UI Components")]
    public Slider hpSlider;          
    public TextMeshProUGUI hpText;   

    [Header("Health Stats")]
    public float maxHP = 100f;
    private float currentHP;

    private PlayerStats playerStats; 

    void Start()
    {
        playerStats = GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            maxHP = playerStats.currentMaxHp; // Lấy Max HP chính xác từ PlayerStats
        }

        currentHP = maxHP; // Gán đầy máu khi bắt đầu
        UpdateHPUI();
    }

    void Update()
    {
        // Đồng bộ Max HP nếu có sự thay đổi từ trang bị
        if (playerStats != null)
        {
            if (maxHP != playerStats.currentMaxHp)
            {
                float oldMaxHP = maxHP;
                maxHP = playerStats.currentMaxHp;
                currentHP = Mathf.Clamp(currentHP + (maxHP - oldMaxHP), 0, maxHP);
                UpdateHPUI();
            }
        }
    }

    public void TakeDamage(float damageAmount)
    {
        currentHP -= damageAmount;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP); 
        UpdateHPUI();

        if (currentHP <= 0)
        {
            Die();
        }
    }

    public void Heal(float healAmount)
    {
        currentHP += healAmount;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        UpdateHPUI();
    }

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
        Debug.Log("Nhân vật đã ngã xuống trong vòng vây của zombie!");
    }
}