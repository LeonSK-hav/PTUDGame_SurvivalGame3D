using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerProfileUI : MonoBehaviour
{
    [Header("References to Player Components")]
    public PlayerStats playerStats;
    public PlayerLevelSystem playerLevelSystem;
    public PlayerHealth playerHealth;

    [Header("Text UI References")]
    public TextMeshProUGUI txtName;
    public TextMeshProUGUI txtLevel;

    public TextMeshProUGUI txtDamage;
    public TextMeshProUGUI txtDefense;
    public TextMeshProUGUI txtAgility;
    public TextMeshProUGUI txtHp;
    public TextMeshProUGUI txtExp;

    [Header("Slider UI References")]
    public Slider hpSlider;
    public Slider expSlider;

    private void Start()
    {
        // Automatically find components on the same GameObject if they are not assigned in the Inspector
        if (playerStats == null) playerStats = GetComponent<PlayerStats>();
        if (playerLevelSystem == null) playerLevelSystem = GetComponent<PlayerLevelSystem>();
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        // Continuously update the profile UI so it reflects real-time changes (stats, HP, EXP, level)
        UpdateProfileUI();
    }

    public void UpdateProfileUI()
    {
        // 1. Display Player Name
        if (txtName != null && playerStats != null)
        {
            txtName.text = playerStats.playerName;
        }
        if (txtLevel != null && playerLevelSystem != null)
        {
            txtLevel.text = $"Lv.{playerLevelSystem.level.ToString()}";
        }

        // 2. Display Combat Stats (Base + Bonus format)
        // Assuming base stats are stored in playerStats and current stats minus base stats represent equipment/level bonuses.
        if (playerStats != null)
        {
           if (txtDamage != null)
            {
                float bonusDamage = playerStats.currentDamage - playerStats.baseDamage;
                if (bonusDamage > 0)
                    txtDamage.text = $"Damage: {playerStats.baseDamage} (+{bonusDamage})";
                else
                    txtDamage.text = $"Damage: {playerStats.baseDamage}";
            }

            if (txtDefense != null)
            {
                float bonusDefense = playerStats.currentDefense - playerStats.baseDefense;
                if (bonusDefense > 0)
                    txtDefense.text = $"Defense: {playerStats.baseDefense} (+{bonusDefense})";
                else
                    txtDefense.text = $"Defense: {playerStats.baseDefense}";
            }

            if (txtAgility != null)
            {
                float bonusAgility = playerStats.currentAgility - playerStats.baseAgility;
                if (bonusAgility > 0)
                    txtAgility.text = $"Agility: {playerStats.baseAgility} (+{bonusAgility})";
                else
                    txtAgility.text = $"Agility: {playerStats.baseAgility}";
            }
        }

        // 3. Display HP and HP Slider
        if (playerHealth != null)
        {
            float currentHp = playerHealth.CurrentHealth;
            float maxHp = playerHealth.MaxHealth;

            if (txtHp != null)
            {
                txtHp.text = $"{Mathf.Ceil(currentHp)} / {maxHp} HP";
            }

            if (hpSlider != null)
            {
                hpSlider.maxValue = maxHp;
                hpSlider.value = currentHp;
            }
        }

        // 4. Display EXP and EXP Slider
        if (playerLevelSystem != null)
        {
            float currentExp = playerLevelSystem.currentExp;
            float expToNextLevel = playerLevelSystem.expToNextLevel;

            if (txtExp != null)
            {
                txtExp.text = $"{currentExp} / {expToNextLevel} Exp";
            }

            if (expSlider != null)
            {
                expSlider.maxValue = expToNextLevel;
                expSlider.value = currentExp;
            }
        }
    }
}