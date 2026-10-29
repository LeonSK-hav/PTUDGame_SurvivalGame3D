using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class InventorySlot
{
    public ItemData itemData;
    public int count;
}

public class InventoryManager : MonoBehaviour, IDragHandler, IEndDragHandler
{
    [Header("UI References (Theo cấu trúc của bạn)")]
    public Transform slotsContainer;     // Chứa các btnSlot trong Panel lưới
    public TextMeshProUGUI txtItemName;  // txtItemName
    public TextMeshProUGUI txtItemUsage; // txtItemUsage
    public TextMeshProUGUI txtCount;     // txtCount (hiển thị số lượng trên slot hoặc bảng chi tiết)
    public Button btnUse;                // btnUse
    public RectTransform inventoryPanelRect; // Khung Panel tổng của Inventory để kiểm tra vứt đồ ra ngoài

    [Header("Inventory Data")]
    public int totalSlots = 35;          // Số ô trong lưới (ví dụ lưới 7x5 = 35 ô)
    public List<InventorySlot> slots = new List<InventorySlot>();

    private int selectedSlotIndex = -1;
    private GameObject draggedIconClone;   // Dùng cho hiệu ứng kéo thả vứt đồ

    void Start()
    {
        // Khởi tạo danh sách slot rỗng
        for (int i = 0; i < totalSlots; i++)
        {
            slots.Add(new InventorySlot());
        }

        // Đăng ký sự kiện nút Sử dụng
        if (btnUse != null)
        {
            btnUse.onClick.AddListener(OnUseItemButtonClicked);
        }

        UpdateInventoryUI();
    }

    // Thêm vật phẩm vào kho (Giới hạn tối đa 99 mỗi slot)
    public bool AddItem(ItemData newItem, int amount)
    {
        // 1. Tìm xem đã có item đó chưa để cộng dồn (nếu không phải equipment và chưa đạt 99)
        if (newItem.itemType != ItemType.Equipment)
        {
            foreach (var slot in slots)
            {
                if (slot.itemData == newItem && slot.count < 99)
                {
                    int spaceLeft = 99 - slot.count;
                    if (amount <= spaceLeft)
                    {
                        slot.count += amount;
                        UpdateInventoryUI();
                        return true;
                    }
                    else
                    {
                        slot.count = 99;
                        amount -= spaceLeft;
                    }
                }
            }
        }

        // 2. Nếu còn dư hoặc là item mới, tìm ô trống tiếp theo
        foreach (var slot in slots)
        {
            if (slot.itemData == null)
            {
                slot.itemData = newItem;
                slot.count = Mathf.Min(amount, 99);
                UpdateInventoryUI();
                return true;
            }
        }

        Debug.Log("Inventory đã đầy!");
        return false;
    }

    // Cập nhật hiển thị lên giao diện UI
    public void UpdateInventoryUI()
    {
        // Duyệt qua các button slot trong UI để gán hình ảnh và số lượng
        for (int i = 0; i < slotsContainer.childCount; i++)
        {
            if (i >= slots.Count) break;

            Transform slotTransform = slotsContainer.GetChild(i);
            Transform imgItemTransform = slotTransform.Find("imgItem");
            Transform txtCountTransform = slotTransform.Find("txtCount");

            Image imgItem = imgItemTransform != null ? imgItemTransform.GetComponent<Image>() : null;
            TextMeshProUGUI countText = txtCountTransform != null ? txtCountTransform.GetComponent<TextMeshProUGUI>() : null;

            var slotData = slots[i];
            if (slotData.itemData != null)
            {
                if (imgItem != null)
                {
                    imgItem.gameObject.SetActive(true);
                    imgItem.sprite = slotData.itemData.itemIcon;
                    // Đảm bảo màu sắc hiển thị rõ nét (Alpha = 1)
    Color col = imgItem.color;
    col.a = 1f;
    imgItem.color = col;
                }
                if (countText != null)
                {
                    countText.gameObject.SetActive(true);
                    countText.text = slotData.count > 1 ? slotData.count.ToString() : "";
                }
            }
            else
            {
                if (imgItem != null) imgItem.gameObject.SetActive(false);
                if (countText != null) countText.gameObject.SetActive(false);
            }
        }
    }

    // Người dùng bấm vào một ô slot để xem thông tin
    public void OnSelectSlot(int index)
    {
        if (index < 0 || index >= slots.Count) return;
        selectedSlotIndex = index;
        var slot = slots[index];

        if (slot.itemData != null)
        {
            if (txtItemName != null) txtItemName.text = slot.itemData.itemName;
            if (txtItemUsage != null) txtItemUsage.text = slot.itemData.itemUsageDescription;
            if (txtCount != null) txtCount.text = slot.count.ToString();

            // Nếu là trang bị (Equipment), ẩn nút Sử dụng (không được dùng bằng nút, phải kéo vào ô equipment)
            if (btnUse != null)
            {
                btnUse.gameObject.SetActive(slot.itemData.itemType != ItemType.Equipment);
            }
        }
        else
        {
            ClearDetailsPanel();
        }
    }

    // Người dùng bấm nút Sử dụng (hoặc bấm nhiều lần để tăng số lượng sử dụng/tiêu thụ)
    void OnUseItemButtonClicked()
    {
        if (selectedSlotIndex < 0 || selectedSlotIndex >= slots.Count) return;
        var slot = slots[selectedSlotIndex];

        if (slot.itemData == null || slot.itemData.itemType == ItemType.Equipment) return;

        // Tác dụng lên nhân vật (Ví dụ: Hồi máu, tăng sát thương)
        ApplyItemEffect(slot.itemData);

        // Giảm số lượng item sau khi sử dụng (nếu không phải item vĩnh cửu như dao/súng đặc biệt)
        if (slot.itemData.itemType == ItemType.Consumable)
        {
            slot.count--;
            if (slot.count <= 0)
            {
                slot.itemData = null;
                slot.count = 0;
                ClearDetailsPanel();
            }
        }

        UpdateInventoryUI();
    }

    void ApplyItemEffect(ItemData item)
    {
        Debug.Log($"Đã sử dụng {item.itemName}. Tác dụng: Hồi {item.healAmount} HP.");
        // TODO: Gọi script nhân vật để cộng máu hoặc tăng chỉ số tại đây
    }

    void ClearDetailsPanel()
    {
        if (txtItemName != null) txtItemName.text = "";
        if (txtItemUsage != null) txtItemUsage.text = "";
        if (txtCount != null) txtCount.text = "";
        if (btnUse != null) btnUse.gameObject.SetActive(false);
    }

    // --- CƠ CHẾ KÉO THẢ VỨT ĐỒ RA NGOÀI MÀN HÌNH ---
    public void OnDrag(PointerEventData eventData)
    {
        // Xử lý logic kéo icon vật phẩm nếu muốn hiển thị hình ảnh đang kéo
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Kiểm tra nếu vị trí kết thúc kéo nằm NGOÀI khung inventoryPanelRect thì tiến hành vứt bỏ item
        if (inventoryPanelRect != null && !RectTransformUtility.RectangleContainsScreenPoint(inventoryPanelRect, eventData.position, eventData.pressEventCamera))
        {
            if (selectedSlotIndex >= 0 && selectedSlotIndex < slots.Count)
            {
                Debug.Log($"Đã vứt vật phẩm {slots[selectedSlotIndex].itemData.itemName} ra ngoài màn hình Inventory!");
                slots[selectedSlotIndex].itemData = null;
                slots[selectedSlotIndex].count = 0;
                ClearDetailsPanel();
                UpdateInventoryUI();
            }
        }
    }
}