using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;       
using TMPro;                

public class PlayerStats : MonoBehaviour
{
    [Header("Thông tin cơ bản")]
    public string playerName = "PlayerName"; 
    public int level = 1;
    public float currentExp = 0f;
    public float expToNextLevel = 100f;

    [Header("UI References")]
    public Slider expSlider;          
    public TextMeshProUGUI expText;   
    public TextMeshProUGUI nameText;  
    public TextMeshProUGUI levelText; 

    [Header("Thuộc tính cơ bản (Gốc)")]
    public float baseMaxHp = 100f;
    public float baseDamage = 10f;
    public float baseDefense = 5f;
    public float baseAgility = 10f;

    [Header("Thuộc tính hiện tại (Đã cộng dồn trang bị)")]
    public float currentMaxHp;
    public float currentDamage;
    public float currentDefense;
    public float currentAgility;

    // Biến lưu trữ tạm để kiểm tra sự thay đổi liên tục
    private float lastExp;
    private int lastLevel;

    private void Start()
    {
        RecalculateStats();
        UpdatePlayerUI(); 

        // Lưu giá trị ban đầu để so sánh
        lastExp = currentExp;
        lastLevel = level;
    }

    // Giữ lại hàm Update để cập nhật liên tục giao diện khi có biến động
    private void Update()
    {
        // Kiểm tra nếu Exp hoặc Level thay đổi (ví dụ bạn chỉnh trực tiếp trên Inspector khi đang Play để test)
        // thì tự động cập nhật lại UI ngay lập tức mà không gọi hàm RecalculateStats() liên tục gây lỗi HP.
        if (currentExp != lastExp || level != lastLevel)
        {
            lastExp = currentExp;
            lastLevel = level;
            UpdatePlayerUI();
        }
    }

    public void AddExp(float amount)
    {
        currentExp += amount;
        while (currentExp >= expToNextLevel)
        {
            currentExp -= expToNextLevel;
            LevelUp();
        }
        UpdatePlayerUI();
    }

    void LevelUp()
    {
        level++;
        expToNextLevel *= 1.2f; 
        
        baseMaxHp += 20f;
        baseDamage += 5f;
        baseDefense += 2f;
        baseAgility += 2f;

        RecalculateStats();
        Debug.Log($"Chúc mừng! Bạn đã lên level {level}");
    }

    public void UpdatePlayerUI()
    {
        if (expSlider != null)
        {
            expSlider.maxValue = expToNextLevel;
            expSlider.value = currentExp;
        }

        if (expText != null)
        {
            expText.text = $"{currentExp} / {expToNextLevel}";
        }

        if (nameText != null)
        {
            nameText.text = $"{playerName}";
        }

        if (levelText != null)
        {
            levelText.text = $"Lv.{level}";
        }
    }

    public void EquipItem(ItemData item)
    {
        if (item == null) return;
        if (item.itemType != ItemType.Equipment) return; 
        baseMaxHp += item.healAmount;
        baseDamage += item.damageBonus;
        baseDefense += item.defenseBonus;
        baseAgility += item.agilityBonus;
        RecalculateStats();
    }

    public void UnequipItem(ItemData item)
    {
        if (item == null) return;
        baseMaxHp -= item.healAmount;
        baseDamage -= item.damageBonus;
        baseDefense -= item.defenseBonus;
        baseAgility -= item.agilityBonus;

        baseMaxHp = Mathf.Max(1f, baseMaxHp);
        baseDamage = Mathf.Max(0f, baseDamage);
        baseDefense = Mathf.Max(0f, baseDefense);
        baseAgility = Mathf.Max(0f, baseAgility);
        RecalculateStats();
    }

    public void RecalculateStats()
    {
        currentMaxHp = baseMaxHp;
        currentDamage = baseDamage;
        currentDefense = baseDefense;
        currentAgility = baseAgility;
        
        UpdatePlayerUI();
    }
}
