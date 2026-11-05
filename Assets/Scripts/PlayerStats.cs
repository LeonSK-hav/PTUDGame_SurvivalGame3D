using UnityEngine;
using TMPro;

public class PlayerStats : MonoBehaviour
{
    [Header("Thông tin cơ bản")]
    public string playerName = "PlayerName"; 

    [Header("UI References")]
    public TextMeshProUGUI nameText;  
    public TextMeshProUGUI coinText;  

    [Header("Vàng")]
    public int coins = 1000;

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

    private void Start()
    {
        RecalculateStats();
        UpdateUI(); 
    }

    public void UpdateUI()
    {
        if (nameText != null)
        {
            nameText.text = $"{playerName}";
        }
        if (coinText != null)
        {
            coinText.text = $"{coins}";
        }
    }

    // Hàm nhận yêu cầu tăng chỉ số khi PlayerLevelSystem gọi lúc lên cấp
    public void AddBaseStatsOnLevelUp(float hpInc, float dmgInc, float defInc, float agiInc)
    {
        baseMaxHp += hpInc;
        baseDamage += dmgInc;
        baseDefense += defInc;
        baseAgility += agiInc;

        RecalculateStats();
    }

    public void EquipItem(ItemData item)
    {
        if (item == null) return;
        if (item.itemType != ItemType.Equipment) return; 

        PlayerLevelSystem levelSystem = GetComponent<PlayerLevelSystem>();
        if (levelSystem == null)
        {
            levelSystem = PlayerLevelSystem.Instance;
        }

        if (levelSystem == null)
        {
            Debug.LogError("Không tìm thấy PlayerLevelSystem để kiểm tra cấp độ trang bị.", this);
            return;
        }

        if (item.requiredLevel > levelSystem.CurrentLevel)
        {
            Debug.LogWarning("Level của bạn chưa đủ để sử dụng vật phẩm này!", this);
            return;
        }

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
    }
}