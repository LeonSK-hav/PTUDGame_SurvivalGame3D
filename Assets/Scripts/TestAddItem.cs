using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestAddItem : MonoBehaviour
{
    public InventoryManager inventoryManager; // Kéo đối tượng Inventory vào đây
    public ItemData samplePotion;             // Kéo file Item_HealthPotion vào đây

    void Update()
    {
        // Bấm phím 'T' trên bàn phím để test thêm 5 bình thuốc vào kho đồ
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (inventoryManager != null && samplePotion != null)
            {
                inventoryManager.AddItem(samplePotion, 5);
                Debug.Log("Đã thêm 5 bình thuốc vào túi đồ!");
            }
        }
    }
}
