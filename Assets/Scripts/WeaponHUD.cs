using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý giao diện HUD hiển thị thông số Vũ khí, Băng đạn, Đạn dự trữ và Cảnh báo trạng thái.
/// Tự động đồng bộ với WeaponSwitcher và WeaponController.
/// </summary>
public class WeaponHUD : MonoBehaviour
{
    [Header("Weapon Switcher Reference")]
    [Tooltip("Tham chiếu tới WeaponSwitcher trên Player. Nếu để trống sẽ tự động tìm trong scene.")]
    public WeaponSwitcher weaponSwitcher;

    [Header("UI Panels")]
    [Tooltip("Panel tổng của Weapon HUD (có thể ẩn khi không cầm súng)")]
    public GameObject hudContainer;

    [Header("UI Elements")]
    [Tooltip("Hiển thị số lượng đạn (Ví dụ: 8 / 32 hoặc 8 / ∞)")]
    public TextMeshProUGUI ammoText;

    [Tooltip("Hiển thị tên vũ khí hiện tại (Ví dụ: M1911)")]
    public TextMeshProUGUI weaponNameText;

    [Tooltip("Hiển thị icon hình ảnh của vũ khí")]
    public Image weaponIconImage;

    [Tooltip("Hiển thị trạng thái/cảnh báo: RELOADING..., OUT OF AMMO!, PRESS R...")]
    public TextMeshProUGUI statusText;

    [Header("Colors & Styling")]
    public Color normalAmmoColor = Color.white;
    public Color lowAmmoColor = new Color(1f, 0.75f, 0.15f); // Màu vàng cam cảnh báo
    public Color noAmmoColor = new Color(1f, 0.25f, 0.25f);  // Màu đỏ hết đạn
    public Color reloadingColor = new Color(0.2f, 0.8f, 1f); // Màu xanh dương nhạt nạp đạn

    [Header("Auto Setup")]
    [Tooltip("Tự động tìm kiếm hoặc khởi tạo UI nếu các biến trên Inspector chưa được gán")]
    public bool autoFindReferences = true;

    private WeaponController activeWeapon;
    private string lastWeaponName = "";

    private void Awake()
    {
        if (hudContainer == null)
        {
            hudContainer = gameObject;
        }

        if (autoFindReferences)
        {
            ResolveReferences();
        }
    }

    private void Start()
    {
        FindWeaponSwitcherIfNeeded();

        if (weaponSwitcher != null)
        {
            weaponSwitcher.onWeaponChanged += OnWeaponChanged;
            activeWeapon = weaponSwitcher.GetActiveWeapon();
            UpdateWeaponDisplay(activeWeapon);
        }
        else
        {
            // Thử lấy vũ khí trực tiếp nếu có trong scene
            activeWeapon = FindFirstObjectByType<WeaponController>();
            UpdateWeaponDisplay(activeWeapon);
        }
    }

    private void OnEnable()
    {
        if (weaponSwitcher != null)
        {
            weaponSwitcher.onWeaponChanged -= OnWeaponChanged;
            weaponSwitcher.onWeaponChanged += OnWeaponChanged;
        }

        if (weaponSwitcher != null)
        {
            activeWeapon = weaponSwitcher.GetActiveWeapon();
            UpdateWeaponDisplay(activeWeapon);
        }
    }

    private void OnDisable()
    {
        if (weaponSwitcher != null)
        {
            weaponSwitcher.onWeaponChanged -= OnWeaponChanged;
        }
    }

    private void OnDestroy()
    {
        if (weaponSwitcher != null)
        {
            weaponSwitcher.onWeaponChanged -= OnWeaponChanged;
        }
    }

    private void Update()
    {
        FindWeaponSwitcherIfNeeded();

        if (weaponSwitcher != null)
        {
            WeaponController current = weaponSwitcher.GetActiveWeapon();
            if (current != activeWeapon)
            {
                OnWeaponChanged(current);
            }
        }

        UpdateRealtimeAmmoAndStatus();
    }

    /// <summary>
    /// Tìm WeaponSwitcher trong Scene nếu chưa được gán
    /// </summary>
    private void FindWeaponSwitcherIfNeeded()
    {
        if (weaponSwitcher == null)
        {
            if (WeaponSwitcher.Instance != null)
            {
                weaponSwitcher = WeaponSwitcher.Instance;
            }
            else
            {
                weaponSwitcher = FindFirstObjectByType<WeaponSwitcher>();
            }

            if (weaponSwitcher != null)
            {
                weaponSwitcher.onWeaponChanged -= OnWeaponChanged;
                weaponSwitcher.onWeaponChanged += OnWeaponChanged;
                activeWeapon = weaponSwitcher.GetActiveWeapon();
                UpdateWeaponDisplay(activeWeapon);
            }
        }
    }

    /// <summary>
    /// Xử lý khi WeaponSwitcher đổi súng
    /// </summary>
    public void OnWeaponChanged(WeaponController newWeapon)
    {
        activeWeapon = newWeapon;
        UpdateWeaponDisplay(activeWeapon);
    }

    /// <summary>
    /// Cập nhật tên và icon súng khi đổi vũ khí
    /// </summary>
    private void UpdateWeaponDisplay(WeaponController weapon)
    {
        if (weapon == null)
        {
            if (weaponNameText != null) weaponNameText.text = "NO WEAPON";
            if (ammoText != null) ammoText.text = "- / -";
            if (weaponIconImage != null)
            {
                weaponIconImage.sprite = null;
                weaponIconImage.enabled = false;
            }
            if (statusText != null)
            {
                statusText.text = "";
                statusText.gameObject.SetActive(false);
            }
            return;
        }

        // 1. Cập nhật tên vũ khí
        string displayName = "Unknown Gun";
        if (weapon.weaponData != null && !string.IsNullOrEmpty(weapon.weaponData.weaponName))
        {
            displayName = weapon.weaponData.weaponName;
        }
        else
        {
            displayName = weapon.gameObject.name;
            if (displayName.Contains("_")) displayName = displayName.Split('_')[0];
            if (displayName.Contains("(")) displayName = displayName.Split('(')[0].Trim();
        }

        lastWeaponName = displayName;
        if (weaponNameText != null)
        {
            weaponNameText.text = displayName;
        }

        // 2. Cập nhật Icon vũ khí
        Sprite iconSprite = null;
        if (weapon.weaponData != null && weapon.weaponData.weaponIcon != null)
        {
            iconSprite = weapon.weaponData.weaponIcon;
        }
        else
        {
            ItemData sourceItem = weapon.GetSourceItemData();
            if (sourceItem != null && sourceItem.itemIcon != null)
            {
                iconSprite = sourceItem.itemIcon;
            }
        }

        if (weaponIconImage != null)
        {
            if (iconSprite != null)
            {
                weaponIconImage.sprite = iconSprite;
                weaponIconImage.enabled = true;
                weaponIconImage.color = Color.white;
            }
            else
            {
                weaponIconImage.enabled = false;
            }
        }

        // Cập nhật ngay thông số đạn
        UpdateRealtimeAmmoAndStatus();
    }

    /// <summary>
    /// Cập nhật realtime số đạn và trạng thái Reloading / Hết đạn
    /// </summary>
    private void UpdateRealtimeAmmoAndStatus()
    {
        if (activeWeapon == null) return;

        // Xử lý riêng cho Vũ khí Cận chiến (không dùng đạn và không cần nạp)
        if (activeWeapon.IsMeleeWeapon)
        {
            if (ammoText != null)
            {
                ammoText.text = "MELEE";
                ammoText.color = normalAmmoColor;
            }
            if (statusText != null)
            {
                statusText.text = "";
                statusText.gameObject.SetActive(false);
            }
            return;
        }

        int currentInMag = activeWeapon.CurrentBullets;
        int maxInMag = activeWeapon.MaxBulletsInMag;
        int reservedAmmo = activeWeapon.GetTotalReservedAmmo();
        bool isReloading = activeWeapon.IsReloading;

        // 1. Định dạng text hiển thị đạn
        if (ammoText != null)
        {
            if (reservedAmmo >= 0)
            {
                ammoText.text = string.Format("{0} / {1}", currentInMag, reservedAmmo);
            }
            else
            {
                // Dự trữ vô hạn (-1)
                ammoText.text = string.Format("{0} / \u221E", currentInMag);
            }

            // Đổi màu chữ theo lượng đạn còn lại trong băng
            if (currentInMag <= 0)
            {
                ammoText.color = noAmmoColor;
            }
            else if (maxInMag > 0 && currentInMag <= Mathf.CeilToInt(maxInMag * 0.25f))
            {
                ammoText.color = lowAmmoColor;
            }
            else
            {
                ammoText.color = normalAmmoColor;
            }
        }

        // 2. Cập nhật trạng thái / cảnh báo
        if (statusText != null)
        {
            if (isReloading)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "RELOADING...";
                statusText.color = reloadingColor;
            }
            else if (currentInMag == 0 && reservedAmmo == 0)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "OUT OF AMMO!";
                statusText.color = noAmmoColor;
            }
            else if (currentInMag == 0)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "PRESS [R] TO RELOAD";
                statusText.color = lowAmmoColor;
            }
            else
            {
                statusText.text = "";
                statusText.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Tự động tìm kiếm các phần tử UI con theo tên thông dụng
    /// </summary>
    private void ResolveReferences()
    {
        if (ammoText == null)
        {
            ammoText = FindComponentByName<TextMeshProUGUI>("txtAmmo") ??
                       FindComponentByName<TextMeshProUGUI>("AmmoText") ??
                       FindComponentByName<TextMeshProUGUI>("Text_Ammo");
        }

        if (weaponNameText == null)
        {
            weaponNameText = FindComponentByName<TextMeshProUGUI>("txtWeaponName") ??
                             FindComponentByName<TextMeshProUGUI>("WeaponNameText") ??
                             FindComponentByName<TextMeshProUGUI>("Text_WeaponName");
        }

        if (weaponIconImage == null)
        {
            weaponIconImage = FindComponentByName<Image>("imgWeaponIcon") ??
                              FindComponentByName<Image>("WeaponIcon") ??
                              FindComponentByName<Image>("Icon");
        }

        if (statusText == null)
        {
            statusText = FindComponentByName<TextMeshProUGUI>("txtStatus") ??
                         FindComponentByName<TextMeshProUGUI>("StatusText") ??
                         FindComponentByName<TextMeshProUGUI>("Text_Status");
        }

        // Nếu vẫn còn thiếu và đang ở Canvas, tự động cấu hình các UI component mặc định
        if (ammoText == null || weaponNameText == null)
        {
            EnsureFallbackUIComponents();
        }
    }

    private T FindComponentByName<T>(string targetName) where T : Component
    {
        T[] all = GetComponentsInChildren<T>(true);
        foreach (var c in all)
        {
            if (c.gameObject.name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
            {
                return c;
            }
        }
        return null;
    }

    /// <summary>
    /// Tự động tạo các UI element con nếu Panel chưa có sẵn Text/Image
    /// </summary>
    private void EnsureFallbackUIComponents()
    {
        // Tạo container góc nếu chưa có
        RectTransform myRect = GetComponent<RectTransform>();
        if (myRect == null) return;

        // Tìm font mặc định
        TMP_FontAsset defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        if (weaponNameText == null)
        {
            GameObject go = new GameObject("txtWeaponName", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.7f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            weaponNameText = go.GetComponent<TextMeshProUGUI>();
            weaponNameText.text = "M1911";
            weaponNameText.fontSize = 24;
            weaponNameText.fontStyle = FontStyles.Bold;
            weaponNameText.alignment = TextAlignmentOptions.Right;
            weaponNameText.color = Color.white;
            if (defaultFont != null) weaponNameText.font = defaultFont;
        }

        if (ammoText == null)
        {
            GameObject go = new GameObject("txtAmmo", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.25f);
            rt.anchorMax = new Vector2(1f, 0.7f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            ammoText = go.GetComponent<TextMeshProUGUI>();
            ammoText.text = "8 / 32";
            ammoText.fontSize = 36;
            ammoText.fontStyle = FontStyles.Bold;
            ammoText.alignment = TextAlignmentOptions.Right;
            ammoText.color = normalAmmoColor;
            if (defaultFont != null) ammoText.font = defaultFont;
        }

        if (statusText == null)
        {
            GameObject go = new GameObject("txtStatus", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0.25f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            statusText = go.GetComponent<TextMeshProUGUI>();
            statusText.text = "";
            statusText.fontSize = 16;
            statusText.fontStyle = FontStyles.Bold;
            statusText.alignment = TextAlignmentOptions.Right;
            statusText.color = reloadingColor;
            if (defaultFont != null) statusText.font = defaultFont;
            go.SetActive(false);
        }

        if (weaponIconImage == null)
        {
            GameObject go = new GameObject("imgWeaponIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.15f);
            rt.anchorMax = new Vector2(0f, 0.85f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(70, 50);
            rt.anchoredPosition = new Vector2(10, 0);

            weaponIconImage = go.GetComponent<Image>();
            weaponIconImage.preserveAspect = true;
            weaponIconImage.enabled = false;
        }
    }
}
