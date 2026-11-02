using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WorldItem : MonoBehaviour
{
    [Header("Item Info")]
    public ItemData itemData;
    public int count = 1;

    [Header("UI / Visual References")]
    public SpriteRenderer spriteRenderer; // Nếu muốn hiển thị icon 2D bay lơ lửng
    public TextMeshProUGUI worldCountText; // (Tùy chọn) Hiển thị số lượng trên đầu vật phẩm

    private bool canBePickedUp = false;

    // Hàm khởi tạo thông tin cho vật phẩm khi được vứt ra từ Inventory
    public void Init(ItemData data, int amount)
    {
        itemData = data;
        count = amount;

        if (spriteRenderer != null && itemData != null)
        {
            spriteRenderer.sprite = itemData.itemIcon;
        }

        if (worldCountText != null)
        {
            worldCountText.text = count > 1 ? count.ToString() : "";
        }

        // Đợi một chút mới cho phép nhặt (tránh việc vừa vứt ra ngay chân player đã bị nhặt lại lập tức)
        Invoke(nameof(EnablePickup), 0.5f);
    }

    void EnablePickup()
    {
        canBePickedUp = true;
    }

    // Xử lý khi người chơi chạm vào vật phẩm để nhặt
    private void OnTriggerEnter(Collider other)
    {
        if (!canBePickedUp) return;

        // Kiểm tra xem đối tượng va chạm có phải là Player không (dựa vào Tag hoặc Component)
        if (other.CompareTag("Player") || other.GetComponent<PlayerStats>() != null)
        {
            // Tìm InventoryManager trong scene để thêm item vào kho
            InventoryManager inventory = FindFirstObjectByType<InventoryManager>();
            if (inventory != null)
            {
                bool success = inventory.AddItem(itemData, count);
                if (success)
                {
                    Debug.Log($"Đã nhặt lại {itemData.itemName} (Số lượng: {count})");
                    Destroy(gameObject); // Xóa vật phẩm dưới đất sau khi nhặt thành công
                }
                else
                {
                    Debug.Log("Inventory đã đầy, không thể nhặt thêm!");
                }
            }
        }
    }
}