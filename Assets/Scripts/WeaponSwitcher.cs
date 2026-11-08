using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponSwitcher : MonoBehaviour
{
    [Header("Weapons List")]
    [Tooltip("Danh sách vũ khí quản lý. Nếu để trống, script sẽ tự động tìm tất cả WeaponController con.")]
    public List<WeaponController> weapons = new List<WeaponController>();
    [Tooltip("Số lượng slot vũ khí tối đa mà Player có thể mang theo")]
    [Range(1, 9)] public int maxWeaponSlots = 3;

    [Header("Selection")]
    public int selectedWeapon = 0;

    [Header("Audio")]
    public AudioClip switchSound;

    [Header("Scroll Settings")]
    [Tooltip("Độ nhạy ngưỡng cuộn chuột để kích hoạt đổi vũ khí")]
    public float scrollThreshold = 0.1f;

    /// <summary>
    /// Event thông báo khi đổi vũ khí, truyền vào WeaponController mới
    /// </summary>
    public event Action<WeaponController> onWeaponChanged;

    public static WeaponSwitcher Instance { get; private set; }

    public int CurrentWeaponIndex
    {
        get { return selectedWeapon; }
    }

    public int WeaponCount
    {
        get { return weapons != null ? weapons.Count : 0; }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Giữ instance đầu tiên
        }
        else
        {
            Instance = this;
        }

        if (weapons == null || weapons.Count == 0)
        {
            RefreshWeaponList();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        RefreshWeaponList();

        // Tự động kích hoạt vũ khí hợp lệ đầu tiên và ẩn các vũ khí còn lại
        if (weapons.Count > 0)
        {
            int initialIndex = -1;
            for (int i = 0; i < weapons.Count; i++)
            {
                if (CanUseWeapon(i))
                {
                    initialIndex = i;
                    break;
                }
            }

            if (initialIndex < 0) initialIndex = 0;
            selectedWeapon = initialIndex;
            ForceSelectWeapon(selectedWeapon);
        }
    }

    /// <summary>
    /// Tự động quét và nạp tất cả các WeaponController con vào danh sách
    /// </summary>
    public void RefreshWeaponList()
    {
        if (weapons == null)
        {
            weapons = new List<WeaponController>();
        }
        else
        {
            weapons.RemoveAll(w => w == null);
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            WeaponController wc = child.GetComponent<WeaponController>();
            if (wc != null && !weapons.Contains(wc))
            {
                weapons.Add(wc);
            }
        }
    }

    private void Update()
    {
        if (weapons == null || weapons.Count <= 1) return;

        // Chặn đổi vũ khí khi gameplay input bị khoá hoặc cursor tự do
        if (IsSwitchBlocked()) return;

        int previousSelected = selectedWeapon;

        // 1. Kiểm tra cuộn chuột (Mouse ScrollWheel)
        if (Mouse.current != null)
        {
            float scrollY = Mouse.current.scroll.ReadValue().y;
            if (scrollY > scrollThreshold)
            {
                SelectPreviousWeapon();
            }
            else if (scrollY < -scrollThreshold)
            {
                SelectNextWeapon();
            }
        }

        // 2. Kiểm tra Input Action Previous / Next từ Input System
        if (InputManager.Instance != null && InputManager.Instance.playerInput != null)
        {
            var prevAction = InputManager.Instance.playerInput.Player.Previous;
            var nextAction = InputManager.Instance.playerInput.Player.Next;

            if (prevAction.WasPressedThisFrame())
            {
                SelectPreviousWeapon();
            }
            else if (nextAction.WasPressedThisFrame())
            {
                SelectNextWeapon();
            }
        }

        // 3. Kiểm tra phím số trực tiếp (1, 2, 3...)
        CheckNumberKeysInput();
    }

    private void CheckNumberKeysInput()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        Key[] digitKeys = new Key[]
        {
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
            Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
        };

        Key[] numpadKeys = new Key[]
        {
            Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4,
            Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9
        };

        for (int i = 0; i < weapons.Count && i < digitKeys.Length; i++)
        {
            if (kb[digitKeys[i]].wasPressedThisFrame || kb[numpadKeys[i]].wasPressedThisFrame)
            {
                SelectWeapon(i);
                break;
            }
        }
    }

    /// <summary>
    /// Kiểm tra điều kiện chặn đổi súng:
    /// - Khi UI đang mở / Cursor tự do
    /// - Khi vũ khí hiện tại đang trong trạng thái Reload
    /// </summary>
    public bool IsSwitchBlocked()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsGameplayInputBlocked)
        {
            return true;
        }

        if (Cursor.visible || Cursor.lockState != CursorLockMode.Locked)
        {
            return true;
        }

        WeaponController currentWeapon = GetActiveWeapon();
        if (currentWeapon != null && currentWeapon.IsReloading)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Lấy level hiện tại của người chơi từ PlayerLevelSystem
    /// </summary>
    public int GetPlayerCurrentLevel()
    {
        if (PlayerLevelSystem.Instance != null)
        {
            return PlayerLevelSystem.Instance.CurrentLevel;
        }

        PlayerLevelSystem pls = FindFirstObjectByType<PlayerLevelSystem>();
        return pls != null ? pls.CurrentLevel : 1;
    }

    /// <summary>
    /// Kiểm tra người chơi có đủ cấp độ để sử dụng vũ khí ở slot index không
    /// </summary>
    public bool CanUseWeapon(int index)
    {
        if (weapons == null || index < 0 || index >= weapons.Count) return false;
        WeaponController wc = weapons[index];
        if (wc == null) return false;
        if (wc.weaponData == null) return true; // Fallback: không có cấu hình WeaponData -> luôn cho phép

        int playerLevel = GetPlayerCurrentLevel();
        return playerLevel >= wc.weaponData.requiredLevel;
    }

    public void SelectNextWeapon()
    {
        if (weapons == null || weapons.Count <= 1) return;

        int start = selectedWeapon;
        for (int i = 1; i <= weapons.Count; i++)
        {
            int next = (start + i) % weapons.Count;
            if (CanUseWeapon(next))
            {
                SelectWeapon(next);
                return;
            }
        }

        Debug.LogWarning("[WeaponSwitcher] Không có vũ khí nào khác khả dụng hoặc đủ cấp độ để đổi!");
    }

    public void SelectPreviousWeapon()
    {
        if (weapons == null || weapons.Count <= 1) return;

        int start = selectedWeapon;
        for (int i = 1; i <= weapons.Count; i++)
        {
            int prev = (start - i) % weapons.Count;
            if (prev < 0) prev += weapons.Count;

            if (CanUseWeapon(prev))
            {
                SelectWeapon(prev);
                return;
            }
        }

        Debug.LogWarning("[WeaponSwitcher] Không có vũ khí nào khác khả dụng hoặc đủ cấp độ để đổi!");
    }

    /// <summary>
    /// Chuyển đổi sang vũ khí theo index chỉ định (có kiểm tra Level yêu cầu)
    /// </summary>
    public void SelectWeapon(int newIndex)
    {
        if (weapons == null || weapons.Count == 0) return;
        if (newIndex < 0 || newIndex >= weapons.Count) return;

        // Kiểm tra điều kiện level yêu cầu
        if (!CanUseWeapon(newIndex))
        {
            WeaponController target = weapons[newIndex];
            string wName = (target != null && target.weaponData != null) ? target.weaponData.weaponName : $"Slot {newIndex + 1}";
            int reqLevel = (target != null && target.weaponData != null) ? target.weaponData.requiredLevel : 1;
            Debug.LogWarning($"[WeaponSwitcher] Cần Level {reqLevel} để sử dụng {wName}! (Level hiện tại: {GetPlayerCurrentLevel()})");
            return;
        }

        // Nếu chọn đúng vũ khí đang cầm và nó đã active thì không cần đổi lại
        if (newIndex == selectedWeapon && weapons[newIndex] != null && weapons[newIndex].gameObject.activeSelf)
        {
            return;
        }

        if (IsSwitchBlocked()) return;

        ForceSelectWeapon(newIndex);
    }

    /// <summary>
    /// Cưỡng chế bật vũ khí tại slot newIndex và ẩn các vũ khí khác
    /// </summary>
    private void ForceSelectWeapon(int newIndex)
    {
        selectedWeapon = newIndex;

        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i] != null)
            {
                bool isActive = (i == selectedWeapon);
                weapons[i].gameObject.SetActive(isActive);
            }
        }

        if (AudioManager.Instance != null && switchSound != null)
        {
            AudioManager.Instance.PlayEffectSound(switchSound);
        }

        WeaponController active = GetActiveWeapon();
        if (onWeaponChanged != null)
        {
            onWeaponChanged.Invoke(active);
        }
    }

    /// <summary>
    /// Trả về WeaponController của vũ khí hiện tại đang kích hoạt
    /// </summary>
    public WeaponController GetActiveWeapon()
    {
        if (weapons == null || weapons.Count == 0) return null;
        if (selectedWeapon >= 0 && selectedWeapon < weapons.Count)
        {
            return weapons[selectedWeapon];
        }
        return null;
    }

    /// <summary>
    /// Gán hoặc thay đổi cấu hình WeaponData cho một slot vũ khí nhất định
    /// Phục vụ cho việc kết nối với Inventory và hệ thống trang bị (có kiểm tra Level)
    /// </summary>
    public bool EquipWeaponToSlot(int slotIndex, WeaponData newWeaponData)
    {
        if (weapons == null || slotIndex < 0 || slotIndex >= weapons.Count)
        {
            Debug.LogWarning($"Slot index {slotIndex} không hợp lệ trong WeaponSwitcher.", this);
            return false;
        }

        if (newWeaponData != null)
        {
            int playerLevel = GetPlayerCurrentLevel();
            if (newWeaponData.requiredLevel > playerLevel)
            {
                Debug.LogWarning($"[WeaponSwitcher] Level của bạn ({playerLevel}) chưa đủ để trang bị {newWeaponData.weaponName} (Yêu cầu Lv.{newWeaponData.requiredLevel})!");
                return false;
            }
        }

        WeaponController targetWeapon = weapons[slotIndex];
        if (targetWeapon == null)
        {
            Debug.LogWarning($"WeaponController ở slot {slotIndex} là null.", this);
            return false;
        }

        targetWeapon.SetupWeapon(newWeaponData);

        if (slotIndex == selectedWeapon && onWeaponChanged != null)
        {
            onWeaponChanged.Invoke(targetWeapon);
        }

        return true;
    }

    /// <summary>
    /// Trang bị vũ khí từ vật phẩm trong Inventory lên tay người chơi:
    /// - Nếu còn slot trống (weapons.Count < maxWeaponSlots): Tạo slot mới (Slot 2, Slot 3...) và chuyển sang vũ khí đó.
    /// - Nếu tất cả slot đã đầy: Hoán đổi với vũ khí đang cầm trên tay (vũ khí cũ cất về lại Inventory).
    /// </summary>
    public bool EquipWeaponFromInventory(ItemData weaponItem)
    {
        if (weaponItem == null) return false;

        int playerLevel = GetPlayerCurrentLevel();
        if (weaponItem.requiredLevel > playerLevel)
        {
            Debug.LogWarning($"[WeaponSwitcher] Level của bạn ({playerLevel}) chưa đủ để trang bị {weaponItem.itemName} (Yêu cầu Lv.{weaponItem.requiredLevel})!");
            return false;
        }

        RefreshWeaponList();

        // 1. Khởi tạo vũ khí mới (ưu tiên nhân bản từ mẫu trong WeaponHolder để giữ đúng vị trí, animator, muzzle flash)
        WeaponController templateWc = FindTemplateWeapon(weaponItem.itemName);
        WeaponController newWc = null;

        if (templateWc != null)
        {
            GameObject clonedObj = Instantiate(templateWc.gameObject, transform);
            clonedObj.name = $"{weaponItem.itemName}_{Guid.NewGuid().ToString().Substring(0, 4)}";
            clonedObj.transform.localPosition = templateWc.transform.localPosition;
            clonedObj.transform.localRotation = templateWc.transform.localRotation;
            clonedObj.transform.localScale = templateWc.transform.localScale;
            newWc = clonedObj.GetComponent<WeaponController>();
        }
        else if (weaponItem.worldItemPrefab != null)
        {
            GameObject newWeaponObj = Instantiate(weaponItem.worldItemPrefab, transform);
            newWeaponObj.name = $"{weaponItem.itemName}_{Guid.NewGuid().ToString().Substring(0, 4)}";
            newWeaponObj.transform.localPosition = Vector3.zero;
            newWeaponObj.transform.localRotation = Quaternion.identity;

            // Xóa physics/colliders của pickup
            Rigidbody rb = newWeaponObj.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);
            Collider col = newWeaponObj.GetComponent<Collider>();
            if (col != null) Destroy(col);

            newWc = newWeaponObj.GetComponent<WeaponController>();
            if (newWc == null)
            {
                // Nếu là vũ khí cận chiến (không dùng đạn), thêm MeleeController thay vì WeaponController
                if (weaponItem.ammoType == AmmoType.None)
                {
                    newWc = newWeaponObj.AddComponent<MeleeController>();
                }
                else
                {
                    newWc = newWeaponObj.AddComponent<WeaponController>();
                }
            }
        }

        if (newWc == null)
        {
            Debug.LogWarning($"[WeaponSwitcher] Không thể khởi tạo vũ khí cho {weaponItem.itemName}!");
            return false;
        }

        WeaponData wData = FindWeaponDataForItem(weaponItem);
        newWc.SetupWeapon(wData, weaponItem);

        int targetSlotIndex = -1;

        // 2. Nếu còn slot trống: thêm vào slot tiếp theo (Slot 2, Slot 3...)
        if (weapons.Count < maxWeaponSlots)
        {
            weapons.Add(newWc);
            targetSlotIndex = weapons.Count - 1;
            Debug.Log($"<color=cyan><b>[WeaponSwitcher] Đã trang bị thành công {weaponItem.itemName} vào Slot {targetSlotIndex + 1}! Tổng số súng đang mang: {weapons.Count}/{maxWeaponSlots}</b></color>");
        }
        else
        {
            // 3. Nếu các slot đã đầy: Hoán đổi với vũ khí đang cầm trên tay
            targetSlotIndex = Mathf.Clamp(selectedWeapon, 0, weapons.Count - 1);
            WeaponController oldWc = weapons[targetSlotIndex];
            if (oldWc != null)
            {
                ItemData oldItemData = oldWc.GetSourceItemData();
                if (oldItemData != null && InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.AddItem(oldItemData, 1);
                    Debug.Log($"<color=yellow>[WeaponSwitcher] Đã cất vũ khí cũ ở Slot {targetSlotIndex + 1} ({oldItemData.itemName}) về lại túi đồ!</color>");
                }
                Destroy(oldWc.gameObject);
            }

            weapons[targetSlotIndex] = newWc;
            Debug.Log($"<color=cyan><b>[WeaponSwitcher] Các slot đã đầy ({maxWeaponSlots}/{maxWeaponSlots}), đã hoán đổi Slot {targetSlotIndex + 1} với {weaponItem.itemName}!</b></color>");
        }

        // 4. Kích hoạt vũ khí vừa trang bị
        ForceSelectWeapon(targetSlotIndex);
        return true;
    }

    /// <summary>
    /// Tháo vũ khí tại slotIndex về lại túi đồ Inventory
    /// (Sẵn sàng phục vụ cho việc kéo thả/tháo trang bị UI sau này)
    /// </summary>
    public bool UnequipWeapon(int slotIndex)
    {
        if (weapons == null || slotIndex < 0 || slotIndex >= weapons.Count) return false;

        WeaponController wc = weapons[slotIndex];
        if (wc == null) return false;

        ItemData itemData = wc.GetSourceItemData();
        if (itemData != null && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(itemData, 1);
        }

        weapons.RemoveAt(slotIndex);
        Destroy(wc.gameObject);

        if (weapons.Count > 0)
        {
            int newSelected = Mathf.Clamp(selectedWeapon, 0, weapons.Count - 1);
            ForceSelectWeapon(newSelected);
        }
        else
        {
            selectedWeapon = 0;
            if (onWeaponChanged != null)
            {
                onWeaponChanged.Invoke(null);
            }
        }

        Debug.Log($"<color=yellow>[WeaponSwitcher] Đã tháo vũ khí ở Slot {slotIndex + 1} về túi đồ. Số vũ khí còn lại: {weapons.Count}</color>");
        return true;
    }

    private WeaponController FindTemplateWeapon(string itemName)
    {
        string clean = itemName.Trim().ToLower();

        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i] == null) continue;
            string wName = weapons[i].weaponData != null ? weapons[i].weaponData.weaponName : weapons[i].name;
            if (wName.Trim().ToLower().Contains(clean) || clean.Contains(wName.Trim().ToLower()))
            {
                return weapons[i];
            }
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            WeaponController wc = child.GetComponent<WeaponController>();
            if (wc != null)
            {
                string wName = wc.weaponData != null ? wc.weaponData.weaponName : child.name;
                if (wName.Trim().ToLower().Contains(clean) || clean.Contains(wName.Trim().ToLower()))
                {
                    return wc;
                }
            }
        }

        return null;
    }

    private WeaponData FindWeaponDataForItem(ItemData item)
    {
        if (item == null) return null;
        WeaponData wData = Resources.Load<WeaponData>($"Weapons/WeaponData_{item.itemName}");
#if UNITY_EDITOR
        if (wData == null)
        {
            wData = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponData>($"Assets/Items/WeaponData_{item.itemName}.asset");
        }
#endif
        return wData;
    }

    /// <summary>
    /// Tìm và trang bị vũ khí theo tên từ Inventory
    /// </summary>
    public bool EquipWeaponByName(string weaponName)
    {
        if (string.IsNullOrEmpty(weaponName)) return false;
        ItemData dummy = ScriptableObject.CreateInstance<ItemData>();
        dummy.itemName = weaponName;
        return EquipWeaponFromInventory(dummy);
    }
}
