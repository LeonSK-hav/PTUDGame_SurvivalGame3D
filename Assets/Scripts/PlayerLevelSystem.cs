using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerLevelSystem : MonoBehaviour
{
    public static PlayerLevelSystem Instance { get; private set; }

    [Header("Level Settings")]
    public int level = 1;
    public float currentExp = 0f;
    public float expToNextLevel = 100f;
    public int CurrentLevel => level;

    [Header("UI References")]
    public Slider expSlider;
    public TextMeshProUGUI expText;
    public TextMeshProUGUI levelText;

    private PlayerStats playerStats;
    private PlayerHealth playerHealth;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        level = Mathf.Max(1, level);
        currentExp = Mathf.Max(0f, currentExp);
        expToNextLevel = Mathf.Max(1f, expToNextLevel);
        playerStats = GetComponent<PlayerStats>();
        playerHealth = GetComponent<PlayerHealth>();
    }

    private void Start()
    {
        UpdateLevelUI();
    }

    public void AddExp(float amount)
    {
        if (float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0f)
        {
            Debug.LogWarning($"EXP reward must be a positive finite value. Received: {amount}", this);
            return;
        }

        currentExp += amount;
        while (currentExp >= expToNextLevel)
        {
            currentExp -= expToNextLevel;
            LevelUp();
        }
        UpdateLevelUI();
    }

    private void LevelUp()
    {
        level++;
        expToNextLevel *= 1.2f; 

        if (playerStats != null)
        {
            playerStats.AddBaseStatsOnLevelUp(20f, 5f, 2f, 2f);
        }
        else
        {
            Debug.LogError("Không tìm thấy PlayerStats để cộng chỉ số khi lên cấp.", this);
        }

        if (playerHealth != null)
        {
            playerHealth.SyncMaxHealth();
        }
        else
        {
            Debug.LogError("Không tìm thấy PlayerHealth để đồng bộ HP khi lên cấp.", this);
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

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}