using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerLevelSystem : MonoBehaviour
{
    [Header("Level Settings")]
    public int level = 1;
    public float currentExp = 0f;
    public float expToNextLevel = 100f;

    [Header("UI References")]
    public Slider expSlider;
    public TextMeshProUGUI expText;
    public TextMeshProUGUI levelText;

    private PlayerStats playerStats;

    private void Start()
    {
        playerStats = GetComponent<PlayerStats>();
        UpdateLevelUI();
    }

    public void AddExp(float amount)
    {
        currentExp += amount;
        while (currentExp >= expToNextLevel)
        {
            currentExp -= expToNextLevel;
            LevelUp();
        }
        UpdateLevelUI();
    }

    void LevelUp()
    {
        level++;
        expToNextLevel *= 1.2f; 

        // Nếu khi lên cấp muốn tự động cộng thêm chỉ số cơ bản vào PlayerStats, có thể gọi hàm tăng chỉ số ở đây
        if (playerStats != null)
        {
            playerStats.AddBaseStatsOnLevelUp(20f, 5f, 2f, 2f); // Tăng: HP, Damage, Defense, Agility
        }

        Debug.Log($"Chúc mừng! Bạn đã lên level {level}");
    }

    public void UpdateLevelUI()
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

        if (levelText != null)
        {
            levelText.text = $"Lv.{level}";
        }
    }
}