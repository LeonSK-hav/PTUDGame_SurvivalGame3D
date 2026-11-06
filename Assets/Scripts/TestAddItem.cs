using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestAddItem : MonoBehaviour
{
    public InventoryManager inventoryManager; // Kéo đối tượng Inventory vào đây
    public ItemData samplePotion;             // Kéo file Item_HealthPotion hoặc Apple vào đây
    public ItemData sampleGun;                // Kéo file M1911 vào đây
    public ItemData sampleAmmo;               // Kéo file SM_Chuck_Pistol_Box_9.19 vào đây

    void Start()
    {
        if (inventoryManager == null)
        {
            inventoryManager = InventoryManager.Instance != null ? InventoryManager.Instance : FindFirstObjectByType<InventoryManager>();
        }
#if UNITY_EDITOR
        if (sampleGun == null)
            sampleGun = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Items/M1911.asset");
        if (sampleAmmo == null)
            sampleAmmo = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Items/SM_Chuck_Pistol_Box_9.19.asset");
        if (samplePotion == null)
            samplePotion = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Items/Apple_01.asset");
#endif
    }

    void Update()
    {
        if (inventoryManager == null)
        {
            inventoryManager = InventoryManager.Instance != null ? InventoryManager.Instance : FindFirstObjectByType<InventoryManager>();
        }

        // Bấm phím 'T' để test thêm 5 bình thuốc / thức ăn vào kho đồ
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (inventoryManager != null && samplePotion != null)
            {
                inventoryManager.AddItem(samplePotion, 5);
                Debug.Log("<color=green>[TestAddItem] Đã thêm 5 vật phẩm hồi phục vào túi đồ!</color>");
            }
        }

        // Bấm phím 'Y' để thêm 32 viên đạn 9x19mm vào kho đồ
        if (Input.GetKeyDown(KeyCode.Y))
        {
            if (inventoryManager != null && sampleAmmo != null)
            {
                inventoryManager.AddItem(sampleAmmo, 32);
                Debug.Log("<color=yellow>[TestAddItem] Đã thêm 32 viên đạn 9x19mm vào túi đồ!</color>");
            }
        }

        // Bấm phím 'U' để thêm 1 khẩu súng M1911 vào kho đồ
        if (Input.GetKeyDown(KeyCode.U))
        {
            if (inventoryManager != null && sampleGun != null)
            {
                inventoryManager.AddItem(sampleGun, 1);
                Debug.Log("<color=cyan>[TestAddItem] Đã thêm 1 khẩu M1911 vào túi đồ!</color>");
            }
        }

        // Bấm phím 'L' để cộng 50 EXP (test cấp độ)
        if (Input.GetKeyDown(KeyCode.L))
        {
            PlayerLevelSystem pls = PlayerLevelSystem.Instance != null ? PlayerLevelSystem.Instance : FindFirstObjectByType<PlayerLevelSystem>();
            if (pls != null)
            {
                pls.AddExp(50f);
                Debug.Log($"<color=magenta>[TestAddItem] Đã cộng 50 EXP! Level hiện tại: {pls.CurrentLevel}</color>");
            }
        }
    }
}
