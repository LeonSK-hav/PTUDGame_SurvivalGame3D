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
    public GameObject worldItemPrefab;          // Kéo Prefab WorldItem vừa tạo ở Bước 1 vào đây!

    [Header("Player References")]
    public PlayerHealth playerHealth;

    private int selectedSlotIndex = -1;          // Ô đang được chọn để xem chi tiết
    private int draggedSlotIndex = -1;           // Ô đang bị kéo chuột
    private GameObject draggedIconClone;         // Bản sao icon đi theo chuột khi kéo thả
    private CanvasGroup cloneCanvasGroup;

    private void OnEnable()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetUIOpen(gameObject, true);
        }
    }

    private void OnDisable()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetUIOpen(gameObject, false);
        }
    }

    void Start()
    {
        if (slots == null)
        {
            slots = new List<InventorySlot>();
        }

        InitSlotsIfNeeded();

        if (btnUse != null)
        {
            btnUse.onClick.AddListener(OnUseItemButtonClicked);
            btnUse.gameObject.SetActive(false);
        }

        if (slotsContainer != null)
        {
            for (int i = 0; i < slotsContainer.childCount; i++)
            {
                int index = i;
                Button slotButton = slotsContainer.GetChild(i).GetComponent<Button>();
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

        if (playerHealth == null && playerTransform != null)
        {
            playerHealth = playerTransform.GetComponentInParent<PlayerHealth>();
        }

        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        }

        RefreshUI();
    }
    // Hàm đảm bảo danh sách slots luôn sẵn sàng dù Inventory chưa mở
    private void InitSlotsIfNeeded()
    {
        if (slots == null)
        {
            slots = new List<InventorySlot>();
        }

        int requiredSlotCount = Mathf.Max(0, totalSlots);
        while (slots.Count < requiredSlotCount)
        {
            slots.Add(new InventorySlot());
        }

        for (int i = 0; i < slots.Count; i++)
        {
            slots[i] ??= new InventorySlot();
        }
    }
    // Thêm vật phẩm vào kho
    public bool AddItem(ItemData newItem, int amount)
    {
        if (newItem == null || amount <= 0 || newItem.maxStack <= 0) return false;

        InitSlotsIfNeeded();

        long availableSpace = 0;
        if (newItem.itemType != ItemType.Equipment)
        {
            foreach (var slot in slots)
            {
                if (slot != null && slot.itemData == newItem && slot.count > 0 && slot.count < newItem.maxStack)
                {
                    availableSpace += newItem.maxStack - slot.count;
                }
            }
        }

        foreach (var slot in slots)
        {
            if (slot != null && (slot.itemData == null || slot.count <= 0))
            {
                availableSpace += newItem.maxStack;
            }
        }

        if (availableSpace < amount)
        {
            Debug.Log("Inventory đã đầy thực sự!");
            return false;
        }

        int remaining = amount;
        if (newItem.itemType != ItemType.Equipment)
        {
            foreach (var slot in slots)
            {
                if (slot == null || slot.itemData != newItem || slot.count <= 0 || slot.count >= newItem.maxStack)
                {
                    continue;
                }

                int added = Mathf.Min(remaining, newItem.maxStack - slot.count);
                slot.count += added;
                remaining -= added;
                if (remaining == 0) break;
            }
        }

        foreach (var slot in slots)
        {
            if (remaining <= 0) break;
            if (slot == null || (slot.itemData != null && slot.count > 0)) continue;

            slot.itemData = newItem;
            slot.count = Mathf.Min(remaining, slot.itemData.maxStack);
            remaining -= slot.count;
        }

        RefreshUI();
        return true;
    }

    public void RefreshUI()
    {
        if (slotsContainer == null)
        {
            Debug.LogError("Chưa gán Slots Container trong InventoryManager.", this);
            RefreshSelectedDetails();
            return;
        }

        if (slotsContainer.childCount != slots.Count)
        {
            Debug.LogError(
                $"Số ô UI ({slotsContainer.childCount}) không khớp số ô dữ liệu ({slots.Count}) trong InventoryManager.",
                this);
        }

        int slotCount = Mathf.Max(slotsContainer.childCount, slots.Count);
        for (int i = 0; i < slotCount; i++)
        {
            InventorySlot slotData = i < slots.Count ? slots[i] : null;
            if (slotData != null && (slotData.itemData == null || slotData.count <= 0))
            {
                slotData.itemData = null;
                slotData.count = 0;
            }

            if (i >= slotsContainer.childCount) continue;

            Transform slotTransform = slotsContainer.GetChild(i);
            Transform imgItemTransform = slotTransform.Find("imgItem");
            Transform txtCountTransform = slotTransform.Find("txtCount");

            Image imgItem = imgItemTransform != null ? imgItemTransform.GetComponent<Image>() : null;
            TextMeshProUGUI countText = txtCountTransform != null ? txtCountTransform.GetComponent<TextMeshProUGUI>() : null;

            bool hasItem = slotData != null && slotData.itemData != null && slotData.count > 0;
            if (imgItem != null)
            {
                imgItem.sprite = hasItem ? slotData.itemData.itemIcon : null;
                imgItem.enabled = hasItem;
                imgItem.gameObject.SetActive(true);
                Color iconColor = imgItem.color;
                iconColor.a = 1f;
                imgItem.color = iconColor;
            }

            if (countText != null)
            {
                countText.text = hasItem && slotData.count > 1 ? slotData.count.ToString() : "";
                countText.enabled = hasItem && slotData.count > 1;
                countText.gameObject.SetActive(true);
            }
        }

        RefreshSelectedDetails();
    }

    public void UpdateInventoryUI()
    {
        RefreshUI();
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
        if (slot.itemData == null || slot.count <= 0)
        {
            ClearDetailsPanel();
            return;
        }

        if (txtItemName != null) txtItemName.text = slot.itemData.itemName;
        if (txtItemUsage != null) txtItemUsage.text = slot.itemData.itemUsageDescription;
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

        if (!ApplyItemEffect(slot.itemData)) return;

        if (slot.itemData.itemType == ItemType.Consumable)
        {
            slot.count--;
            if (slot.count <= 0)
            {
                slot.itemData = null;
                slot.count = 0;
            }
        }

        RefreshUI();
    }

    bool ApplyItemEffect(ItemData item)
    {
        if (item == null)
        {
            Debug.LogError("Không thể sử dụng vật phẩm rỗng.", this);
            return false;
        }

        PlayerLevelSystem levelSystem = PlayerLevelSystem.Instance;
        if (levelSystem == null)
        {
            Debug.LogError("Không tìm thấy PlayerLevelSystem để kiểm tra cấp độ vật phẩm.", this);
            return false;
        }

        if (item.requiredLevel > levelSystem.CurrentLevel)
        {
            Debug.LogWarning("Level của bạn chưa đủ để sử dụng vật phẩm này!", this);
            return false;
        }

        if (item.healAmount > 0f)
        {
            if (playerHealth == null)
            {
                Debug.LogError("Không tìm thấy PlayerHealth để sử dụng vật phẩm hồi máu.", this);
                return false;
            }

            if (!playerHealth.CanHeal)
            {
                Debug.Log("Máu của Player đã đầy, không thể sử dụng vật phẩm hồi máu.");
                return false;
            }

            playerHealth.Heal(item.healAmount);
        }

        Debug.Log($"Đã sử dụng {item.itemName}. Tác dụng: Hồi {item.healAmount} HP.");
        return true;
    }

    void ClearDetailsPanel()
    {
        if (txtItemName != null) txtItemName.text = "";
        if (txtItemUsage != null) txtItemUsage.text = "";
        if (txtCount != null) txtCount.text = "";
        if (btnUse != null) btnUse.gameObject.SetActive(false);
    }

    // --- CƠ CHẾ KÉO THẢ (DRAG & DROP) & VỨT ĐỒ RA THẾ GIỚI ---

    private int GetSlotIndexUnderMouse(Vector2 screenPosition, Camera eventCamera)
    {
        if (slotsContainer == null) return -1;

        for (int i = 0; i < slotsContainer.childCount; i++)
        {
            RectTransform slotRect = slotsContainer.GetChild(i) as RectTransform;
            if (slotRect != null && RectTransformUtility.RectangleContainsScreenPoint(slotRect, screenPosition, eventCamera))
            {
                return i;
            }
        }
        return -1;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        draggedSlotIndex = GetSlotIndexUnderMouse(eventData.position, eventData.pressEventCamera);

        if (draggedSlotIndex >= 0 && draggedSlotIndex < slots.Count && slots[draggedSlotIndex].itemData != null && slots[draggedSlotIndex].count > 0)
        {
            if (canvasTransform != null)
            {
                draggedIconClone = new GameObject("DraggedIconClone");
                draggedIconClone.transform.SetParent(canvasTransform, false);

                Image cloneImage = draggedIconClone.AddComponent<Image>();
                cloneImage.sprite = slots[draggedSlotIndex].itemData.itemIcon;
                cloneImage.raycastTarget = false;
                cloneImage.color = new Color(1f, 1f, 1f, 0.8f);

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
        if (draggedIconClone != null)
        {
            Destroy(draggedIconClone);
        }

        if (draggedSlotIndex < 0) return;

        if (inventoryPanelRect != null && !RectTransformUtility.RectangleContainsScreenPoint(inventoryPanelRect, eventData.position, eventData.pressEventCamera))
        {
            DropItemToWorld(draggedSlotIndex);
            draggedSlotIndex = -1;
            return;
        }

        int targetSlotIndex = GetSlotIndexUnderMouse(eventData.position, eventData.pressEventCamera);
        if (targetSlotIndex >= 0 && targetSlotIndex < slots.Count)
        {
            if (targetSlotIndex != draggedSlotIndex)
            {
                InventorySlot sourceSlot = slots[draggedSlotIndex];
                InventorySlot targetSlot = slots[targetSlotIndex];

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
                    InventorySlot temp = new InventorySlot { itemData = targetSlot.itemData, count = targetSlot.count };
                    targetSlot.itemData = sourceSlot.itemData;
                    targetSlot.count = sourceSlot.count;
                    
                    sourceSlot.itemData = temp.itemData;
                    sourceSlot.count = temp.count;
                }

                RefreshUI();
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

        // Ưu tiên lấy prefab riêng của từng ItemData, nếu không có thì dùng prefab chung của InventoryManager
        GameObject prefabToSpawn = slot.itemData.worldItemPrefab != null ? slot.itemData.worldItemPrefab : worldItemPrefab;

        if (prefabToSpawn == null)
        {
            Debug.LogWarning("Chưa gán World Item Prefab (cả ở ItemData lẫn InventoryManager Inspector)!");
            return;
        }

        if (prefabToSpawn.GetComponent<WorldItem>() == null)
        {
            Debug.LogError("World Item Prefab không có component WorldItem.", prefabToSpawn);
            return;
        }

        ItemData droppedItem = slot.itemData;
        int droppedCount = slot.count;
        slot.itemData = null;
        slot.count = 0;

        Vector3 dropPos = playerTransform != null
            ? playerTransform.position + playerTransform.forward * 2f + Vector3.up * 2.5f
            : transform.position;
        GameObject droppedObj = Instantiate(prefabToSpawn, dropPos, Quaternion.identity);
        WorldItem worldItemComp = droppedObj.GetComponent<WorldItem>();
        if (worldItemComp == null)
        {
            Debug.LogError("World Item Prefab đã tạo không có component WorldItem.", droppedObj);
            Destroy(droppedObj);
            slot.itemData = droppedItem;
            slot.count = droppedCount;
            RefreshUI();
            return;
        }

        worldItemComp.Init(droppedItem, droppedCount);
        RefreshUI();
    }
}