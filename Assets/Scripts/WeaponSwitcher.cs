using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponSwitcher : MonoBehaviour
{
    [Header("Weapons List")]
    [Tooltip("Danh sách vũ khí quản lý. Nếu để trống, script sẽ tự động tìm tất cả WeaponController con.")]
    public List<WeaponController> weapons = new List<WeaponController>();

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
        if (weapons == null || weapons.Count == 0)
        {
            RefreshWeaponList();
        }
    }

    private void Start()
    {
        if (weapons == null || weapons.Count == 0)
        {
            RefreshWeaponList();
        }

        // Tự động kích hoạt vũ khí đầu tiên và ẩn các vũ khí còn lại
        if (weapons.Count > 0)
        {
            selectedWeapon = Mathf.Clamp(selectedWeapon, 0, weapons.Count - 1);
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
            weapons.Clear();
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            WeaponController wc = child.GetComponent<WeaponController>();
            if (wc != null)
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

    public void SelectNextWeapon()
    {
        if (weapons == null || weapons.Count <= 1) return;
        int next = (selectedWeapon + 1) % weapons.Count;
        SelectWeapon(next);
    }

    public void SelectPreviousWeapon()
    {
        if (weapons == null || weapons.Count <= 1) return;
        int prev = selectedWeapon - 1;
        if (prev < 0) prev = weapons.Count - 1;
        SelectWeapon(prev);
    }

    /// <summary>
    /// Chuyển đổi sang vũ khí theo index chỉ định
    /// </summary>
    public void SelectWeapon(int newIndex)
    {
        if (weapons == null || weapons.Count == 0) return;
        if (newIndex < 0 || newIndex >= weapons.Count) return;

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
    /// Phục vụ cho việc kết nối với Inventory và hệ thống trang bị sau này
    /// </summary>
    public bool EquipWeaponToSlot(int slotIndex, WeaponData newWeaponData)
    {
        if (weapons == null || slotIndex < 0 || slotIndex >= weapons.Count)
        {
            Debug.LogWarning($"Slot index {slotIndex} không hợp lệ trong WeaponSwitcher.", this);
            return false;
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
}
