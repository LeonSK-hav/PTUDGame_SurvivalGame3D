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

public class InventoryManager : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI References")]
    public Transform slotsContainer;             // Chứa các btnSlot trong Panel lưới
    public TextMeshProUGUI txtItemName;          // txtItemName
    public TextMeshProUGUI txtItemUsage;         // txtItemUsage
    public TextMeshProUGUI txtCount;             // txtCount (hiển thị "Quantity: X")
    public Button btnUse;                        // btnUse
    public RectTransform inventoryPanelRect;     // Khung Panel tổng để kiểm tra vứt đồ ra ngoài
    public Transform canvasTransform;            // Canvas chính để chứa icon clone khi kéo thả

    [Header("Inventory Data")]
    public int totalSlots = 35;                  // Số ô trong lưới (35 ô)
    public List<InventorySlot> slots = new List<InventorySlot>();

    [Header("World Drop Settings")]
    public Transform playerTransform;            // Vị trí người chơi để sinh vật phẩm ra thế giới

    private int selectedSlotIndex = -1;          // Ô đang được chọn để xem chi tiết
    private int draggedSlotIndex = -1;           // Ô đang bị kéo chuột
    private GameObject draggedIconClone;         // Bản sao icon đi theo chuột khi kéo
    private CanvasGroup cloneCanvasGroup;
    
    void Start()
    {
        // Khởi tạo hoặc làm sạch danh sách slots để tránh bị nhân đôi / lệch index
        if (slots == null)
        {
            slots = new List<InventorySlot>();
        }

        // Đảm bảo danh sách luôn đúng bằng totalSlots (35 ô)
        if (slots.Count != totalSlots)
        {
            slots.Clear();
            for (int i = 0; i < totalSlots; i++)
            {
                slots.Add(new InventorySlot());
            }
        }
        else
        {
            // Kiểm tra các phần tử bên trong xem có bị null không để cấp phát lại an toàn
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] == null)
                {
                    slots[i] = new InventorySlot();
                }
            }
        }

        // 2. Đăng ký sự kiện nút Sử dụng
        if (btnUse != null)
        {
            btnUse.onClick.AddListener(OnUseItemButtonClicked);
            btnUse.gameObject.SetActive(false);
        }

        // 3. Tự động gán sự kiện click cho các ô slot
        if (slotsContainer != null)
        {
            for (int i = 0; i < slotsContainer.childCount; i++)
            {
                int index = i;
                Transform slotTransform = slotsContainer.GetChild(i);
                Button slotButton = slotTransform.GetComponent<Button>();
                
                if (slotButton != null)
                {
                    slotButton.onClick.RemoveAllListeners();
                    slotButton.onClick.AddListener(() => OnSelectSlot(index));
                }
            }
        }

        if (canvasTransform == null)
        {
            canvasTransform = FindObjectOfType<Canvas>()?.transform;
        }

        UpdateInventoryUI();
        ClearDetailsPanel();
    }
    // Hàm đảm bảo danh sách slots luôn sẵn sàng dù Inventory chưa mở
    private void InitSlotsIfNeeded()
    {
        if (slots == null)
        {
            slots = new List<InventorySlot>();
        }

        if (slots.Count != totalSlots)
        {
            slots.Clear();
            for (int i = 0; i < totalSlots; i++)
            {
                slots.Add(new InventorySlot());
            }
        }
        else
        {
            for (int i = 0; i < slots.Count; i++)
            {
                slots[i] ??= new InventorySlot();
            }
        }
    }
    // Thêm vật phẩm vào kho
    public bool AddItem(ItemData newItem, int amount)
    {
        if (newItem == null || amount <= 0) return false;
        // Đảm bảo dữ liệu slots đã sẵn sàng ngay cả khi Inventory chưa từng được mở
        InitSlotsIfNeeded();
        // 1. Kiểm tra và cộng dồn vào các ô đã có sẵn item đó (nếu không phải Equipment và chưa đạt maxStack = 99)
        if (newItem.itemType != ItemType.Equipment)
        {
            foreach (var slot in slots)
            {
                if (slot != null && slot.itemData == newItem && slot.count < 99)
                {
                    int spaceLeft = 99 - slot.count;
                    if (amount <= spaceLeft)
                    {
                        slot.count += amount;
                        UpdateInventoryUI();
                        RefreshSelectedDetails();
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

        // 2. Tìm ô trống thực sự (Sửa điều kiện kiểm tra để tránh lỗi kẹt ô trống)
        foreach (var slot in slots)
        {
            if (slot != null)
            {
                // Nếu slot hoàn toàn trống hoặc count bằng 0 nhưng itemData chưa được gán null
                if (slot.itemData == null || slot.count <= 0)
                {
                    slot.itemData = newItem;
                    slot.count = Mathf.Min(amount, 99);
                    UpdateInventoryUI();
                    RefreshSelectedDetails();
                    return true;
                }
            }
        }

        Debug.Log("Inventory đã đầy thực sự!");
        return false;
    }

    // Cập nhật giao diện Inventory
    public void UpdateInventoryUI()
    {
        for (int i = 0; i < slotsContainer.childCount; i++)
        {
            if (i >= slots.Count) break;

            Transform slotTransform = slotsContainer.GetChild(i);
            Transform imgItemTransform = slotTransform.Find("imgItem");
            Transform txtCountTransform = slotTransform.Find("txtCount");

            Image imgItem = imgItemTransform != null ? imgItemTransform.GetComponent<Image>() : null;
            TextMeshProUGUI countText = txtCountTransform != null ? txtCountTransform.GetComponent<TextMeshProUGUI>() : null;

            var slotData = slots[i];
            if (slotData.itemData != null && slotData.count > 0)
            {
                if (imgItem != null)
                {
                    imgItem.gameObject.SetActive(true);
                    imgItem.sprite = slotData.itemData.itemIcon;
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
                slotData.itemData = null;
                slotData.count = 0;

                if (imgItem != null) imgItem.gameObject.SetActive(false);
                if (countText != null) countText.gameObject.SetActive(false);
            }
        }
    }

    // Chọn ô slot để xem thông tin
    public void OnSelectSlot(int index)
    {
        if (index < 0 || index >= slots.Count) return;
        selectedSlotIndex = index;
        RefreshSelectedDetails();
    }

    // Làm mới thông tin chi tiết item được chọn
    void RefreshSelectedDetails()
    {
        if (selectedSlotIndex < 0 || selectedSlotIndex >= slots.Count)
        {
            ClearDetailsPanel();
            return;
        }

        var slot = slots[selectedSlotIndex];

        // Nếu item trống hoặc số lượng = 0 thì ẩn thông tin
        if (slot.itemData == null || slot.count <= 0)
        {
            ClearDetailsPanel();
            return;
        }

        if (txtItemName != null) txtItemName.text = slot.itemData.itemName;
        if (txtItemUsage != null) txtItemUsage.text = slot.itemData.itemUsageDescription;
        
        // Hiển thị định dạng Quantity: số lượng
        if (txtCount != null) txtCount.text = $"Quantity: {slot.count}";

        if (btnUse != null)
        {
            btnUse.gameObject.SetActive(slot.itemData.itemType != ItemType.Equipment);
        }
    }

    // Nút Sử dụng vật phẩm
    void OnUseItemButtonClicked()
    {
        if (selectedSlotIndex < 0 || selectedSlotIndex >= slots.Count) return;
        var slot = slots[selectedSlotIndex];

        if (slot.itemData == null || slot.count <= 0 || slot.itemData.itemType == ItemType.Equipment) return;

        ApplyItemEffect(slot.itemData);

        if (slot.itemData.itemType == ItemType.Consumable)
        {
            slot.count--;
            if (slot.count <= 0)
            {
                slot.itemData = null;
                slot.count = 0;
                ClearDetailsPanel();
            }
            else
            {
                RefreshSelectedDetails();
            }
        }

        UpdateInventoryUI();
    }

    void ApplyItemEffect(ItemData item)
    {
        Debug.Log($"Đã sử dụng {item.itemName}. Tác dụng: Hồi {item.healAmount} HP.");
    }

    void ClearDetailsPanel()
    {
        if (txtItemName != null) txtItemName.text = "";
        if (txtItemUsage != null) txtItemUsage.text = "";
        if (txtCount != null) txtCount.text = "";
        if (btnUse != null) btnUse.gameObject.SetActive(false);
    }

    // --- CƠ CHẾ KÉO THẢ (DRAG & DROP) & VỨT ĐỒ RA THẾ GIỚI ---

    // Tìm xem con chuột đang ở ô slot nào dựa trên vị trí màn hình
    private int GetSlotIndexUnderMouse(Vector2 screenPosition)
    {
        if (slotsContainer == null) return -1;

        for (int i = 0; i < slotsContainer.childCount; i++)
        {
            RectTransform slotRect = slotsContainer.GetChild(i) as RectTransform;
            if (slotRect != null && RectTransformUtility.RectangleContainsScreenPoint(slotRect, screenPosition, null))
            {
                return i;
            }
        }
        return -1;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        draggedSlotIndex = GetSlotIndexUnderMouse(eventData.position);

        if (draggedSlotIndex >= 0 && draggedSlotIndex < slots.Count && slots[draggedSlotIndex].itemData != null && slots[draggedSlotIndex].count > 0)
        {
            // Tạo bản sao icon đi theo chuột
            if (canvasTransform != null)
            {
                draggedIconClone = new GameObject("DraggedIconClone");
                draggedIconClone.transform.SetParent(canvasTransform, false);

                Image cloneImage = draggedIconClone.AddComponent<Image>();
                cloneImage.sprite = slots[draggedSlotIndex].itemData.itemIcon;
                cloneImage.raycastTarget = false;

                cloneCanvasGroup = draggedIconClone.AddComponent<CanvasGroup>();
                cloneCanvasGroup.blocksRaycasts = false;

                RectTransform cloneRect = draggedIconClone.GetComponent<RectTransform>();
                cloneRect.sizeDelta = new Vector2(60, 60);
                cloneRect.position = eventData.position;
            }
        }
        else
        {
            draggedSlotIndex = -1;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggedIconClone != null)
        {
            RectTransform cloneRect = draggedIconClone.GetComponent<RectTransform>();
            cloneRect.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Xóa bản sao icon kéo thả
        if (draggedIconClone != null)
        {
            Destroy(draggedIconClone);
        }

        if (draggedSlotIndex < 0) return;

        // 1. Kiểm tra xem có thả ra ngoài phạm vi Inventory panel không (Vứt ra thế giới)
        if (inventoryPanelRect != null && !RectTransformUtility.RectangleContainsScreenPoint(inventoryPanelRect, eventData.position, eventData.pressEventCamera))
        {
            DropItemToWorld(draggedSlotIndex);
            draggedSlotIndex = -1;
            return;
        }

        // 2. Thả vào một ô slot khác bên trong Inventory (Đổi vị trí / Swap)
        int targetSlotIndex = GetSlotIndexUnderMouse(eventData.position);
        if (targetSlotIndex >= 0 && targetSlotIndex < slots.Count)
        {
            if (targetSlotIndex != draggedSlotIndex)
            {
                // Thực hiện đổi chỗ hoặc chuyển đồ
                InventorySlot sourceSlot = slots[draggedSlotIndex];
                InventorySlot targetSlot = slots[targetSlotIndex];

                // Nếu ô đích có đồ và cùng loại có thể stack được, cộng dồn số lượng
                if (targetSlot.itemData == sourceSlot.itemData && targetSlot.itemData.itemType != ItemType.Equipment)
                {
                    int totalCount = targetSlot.count + sourceSlot.count;
                    if (totalCount <= 99)
                    {
                        targetSlot.count = totalCount;
                        sourceSlot.itemData = null;
                        sourceSlot.count = 0;
                    }
                    else
                    {
                        targetSlot.count = 99;
                        sourceSlot.count = totalCount - 99;
                    }
                }
                else
                {
                    // Hoán đổi vị trí (Swap) cho nhau (kể cả ô đích trống hay có đồ khác)
                    InventorySlot temp = new InventorySlot { itemData = targetSlot.itemData, count = targetSlot.count };
                    targetSlot.itemData = sourceSlot.itemData;
                    targetSlot.count = sourceSlot.count;
                    
                    sourceSlot.itemData = temp.itemData;
                    sourceSlot.count = temp.count;
                }

                UpdateInventoryUI();
                RefreshSelectedDetails();
            }
        }

        draggedSlotIndex = -1;
    }

    // Sinh vật phẩm ra thế giới khi kéo vứt ra ngoài khung inventory
    void DropItemToWorld(int slotIndex)
    {
        var slot = slots[slotIndex];
        if (slot.itemData == null || slot.count <= 0) return;

        Debug.Log($"Đã vứt vật phẩm {slot.itemData.itemName} (Số lượng: {slot.count}) ra ngoài thế giới!");

        // TODO: Nếu bạn có Prefab rơi ngoài thế giới (ví dụ WorldItem Prefab chứa script nhặt đồ), hãy gán vào đây để Instantiate
        // Ví dụ:
        // if (worldItemPrefab != null)
        // {
        //     Vector3 dropPos = playerTransform != null ? playerTransform.position + playerTransform.forward * 2f : Vector3.zero;
        //     GameObject droppedObj = Instantiate(worldItemPrefab, dropPos, Quaternion.identity);
        //     // Truyền thông tin ItemData và count cho object vừa rơi
        //     droppedObj.GetComponent<WorldItem>()?.Init(slot.itemData, slot.count);
        // }

        // Xóa item khỏi slot sau khi vứt
        slot.itemData = null;
        slot.count = 0;

        UpdateInventoryUI();
        ClearDetailsPanel();
    }
}