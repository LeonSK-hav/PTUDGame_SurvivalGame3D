using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStamina : MonoBehaviour
{
    [Header("Stamina")]
    public float maxStamina = 100f;
    public float currentStamina = 100f;
    public float drainRate = 20f;
    public float regenRate = 15f;
    public float regenDelay = 1f;

    [Header("UI")]
    public Slider staminaSlider;
    public TextMeshProUGUI staminaText;

    private float lastSprintTime;
    private bool isExhausted;

    private void Awake()
    {
        maxStamina = Mathf.Max(0f, maxStamina);
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        drainRate = Mathf.Max(0f, drainRate);
        regenRate = Mathf.Max(0f, regenRate);
        regenDelay = Mathf.Max(0f, regenDelay);
        lastSprintTime = Time.time - regenDelay;
        isExhausted = currentStamina <= 0f;
        UpdateUI();
    }

    public bool UpdateStamina(bool isMoving, bool sprintRequested)
    {
        bool canSprint = isMoving && sprintRequested && !isExhausted && currentStamina > 0f;

        if (canSprint)
        {
            currentStamina = Mathf.Max(0f, currentStamina - drainRate * Time.deltaTime);
            lastSprintTime = Time.time;

            if (currentStamina <= 0f)
            {
                isExhausted = true;
                canSprint = false;
            }
        }
        else
        {
            if (Time.time - lastSprintTime >= regenDelay)
            {
                currentStamina = Mathf.Min(maxStamina, currentStamina + regenRate * Time.deltaTime);
            }

            if (isExhausted && currentStamina > maxStamina * 0.1f)
            {
                isExhausted = false;
            }
        }

        UpdateUI();
        return canSprint;
    }

    private void UpdateUI()
    {
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = currentStamina;
        }

        if (staminaText != null)
        {
            staminaText.text = $"{Mathf.CeilToInt(currentStamina)}/{Mathf.CeilToInt(maxStamina)}";
        }
    }
}
