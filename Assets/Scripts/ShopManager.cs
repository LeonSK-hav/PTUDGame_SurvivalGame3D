using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class ShopItem
{
    public ItemData itemData;
    public int price;         // Giá mua hoặc bán của 1 đơn vị
    public int stock = 99;    // Số lượng tồn kho
}

public class ShopManager : MonoBehaviour
{
    [Header("References")]
    public InventoryManager inventoryManager; // Tham chiếu tới Inventory của player
    public Transform slotsContainer;          // Chứa các btnSlot trong Panel cửa hàng

    [Header("Player Currency")]
    public int playerCoins = 1000;            // Số tiền hiện tại của người chơi
    public TextMeshProUGUI txtYourCoin;       // Hiển thị số tiền của player

    [Header("Transaction UI")]
    public TextMeshProUGUI txtItemName;       // Tên vật phẩm đang chọn
    public TextMeshProUGUI txtItemUsage;      // Mô tả công dụng
    public TextMeshProUGUI txtPrice;          // Tổng giá tiền thanh toán
    public TextMeshProUGUI txtCount;          // Hiển thị dạng "Quantity: X"
    public Button btnAdd;                     // Tăng số lượng
    public Button btnMinus;                   // Giảm số lượng
    public Slider quantitySlider;             // Slider điều chỉnh số lượng
    public Button btnBuy;                     // Nút Mua
    public Button btnSell;                    // Nút Bán

    [Header("Shop Mode")]
    public bool isBuyMode = true;             // True = Shop Mua, False = Shop Bán

    [Header("Shop Inventory Data")]
    public List<ShopItem> shopItems = new List<ShopItem>();

    private int selectedIndex = -1;           
    private int currentQuantity = 1;          
    private int maxQuantity = 1;              

    void Start()
    {
        if (btnAdd != null) btnAdd.onClick.AddListener(OnAddButtonClicked);
        if (btnMinus != null) btnMinus.onClick.AddListener(OnMinusButtonClicked);

        if (quantitySlider != null)
        {
            quantitySlider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        if (btnBuy != null) btnBuy.onClick.AddListener(OnBuyItem);
        if (btnSell != null) btnSell.onClick.AddListener(OnSellItem);

        // Nếu là Shop Bán, tự động đồng bộ danh sách từ Inventory khi bắt đầu
        if (!isBuyMode && inventoryManager != null)
        {
            SyncShopItemsFromInventory();
        }

        CleanAndSortShopList();
        UpdateCoinUI();
        UpdateShopUI();
        ClearDetails();
    }

    // Làm sạch và sắp xếp lại danh sách: Loại bỏ các item hết hàng (stock <= 0) để dồn lên trên, tránh để trống ô
    public void CleanAndSortShopList()
    {
        // Xóa bỏ các mục không hợp lệ hoặc hết hàng
        shopItems.RemoveAll(item => item == null || item.itemData == null || item.stock <= 0);
    }

    // Đồng bộ danh sách từ Inventory qua Shop Bán
    public void SyncShopItemsFromInventory()
    {
        shopItems.Clear();
        if (inventoryManager == null) return;

        foreach (var slot in inventoryManager.slots)
        {
            if (slot.itemData != null && slot.count > 0)
            {
                // Kiểm tra xem đã có trong shopItems chưa để cộng dồn stock
                var existingItem = shopItems.Find(x => x.itemData == slot.itemData);
                if (existingItem != null)
                {
                    existingItem.stock += slot.count;
                }
                else
                {
                    shopItems.Add(new ShopItem
                    {
                        itemData = slot.itemData,
                        price = 50, // Giá bán mặc định
                        stock = slot.count
                    });
                }
            }
        }
        CleanAndSortShopList();
        UpdateShopUI();
    }

    // Cập nhật giao diện Shop và TỰ ĐỘNG ẨN CÁC Ô TRỐNG THỪA
    public void UpdateShopUI()
    {
        if (slotsContainer == null) return;

        CleanAndSortShopList(); // Đảm bảo danh sách luôn gọn gàng trước khi vẽ UI

        for (int i = 0; i < slotsContainer.childCount; i++)
        {
            Transform slotTransform = slotsContainer.GetChild(i);
            Transform imgItemTransform = slotTransform.Find("imgItem");
            Transform txtCountTransform = slotTransform.Find("txtCount");
            Button slotButton = slotTransform.GetComponent<Button>();

            Image imgItem = imgItemTransform != null ? imgItemTransform.GetComponent<Image>() : null;
            TextMeshProUGUI countText = txtCountTransform != null ? txtCountTransform.GetComponent<TextMeshProUGUI>() : null;

            // Nếu ô này có sản phẩm trong danh sách đã dồn lên đầu
            if (i < shopItems.Count && shopItems[i].itemData != null && shopItems[i].stock > 0)
            {
                slotTransform.gameObject.SetActive(true); // Hiển thị ô slot
                var item = shopItems[i];

                if (imgItem != null)
                {
                    imgItem.gameObject.SetActive(true);
                    imgItem.sprite = item.itemData.itemIcon;
                    Color col = imgItem.color;
                    col.a = 1f;
                    imgItem.color = col;
                }
                if (countText != null)
                {
                    countText.gameObject.SetActive(true);
                    countText.text = item.stock > 1 ? item.stock.ToString() : "";
                }

                // Gán sự kiện click cho slot tương ứng
                int index = i;
                if (slotButton != null)
                {
                    slotButton.onClick.RemoveAllListeners();
                    slotButton.onClick.AddListener(() => OnSelectShopSlot(index));
                }
            }
            else
            {
                // ẨN HẲN CÁC Ô TRỐNG KHÔNG CÓ SẢN PHẨM HOẶC THỪA Ở PHÍA SAU
                slotTransform.gameObject.SetActive(false);
            }
        }
    }

    // Chọn sản phẩm trong Shop
    public void OnSelectShopSlot(int index)
    {
        if (index < 0 || index >= shopItems.Count || shopItems[index].itemData == null || shopItems[index].stock <= 0)
        {
            ClearDetails();
            return;
        }

        selectedIndex = index;
        var shopItem = shopItems[selectedIndex];

        if (isBuyMode)
        {
            int affordableCount = shopItem.price > 0 ? playerCoins / shopItem.price : 99;
            maxQuantity = Mathf.Max(1, Mathf.Min(shopItem.stock, affordableCount));
        }
        else
        {
            maxQuantity = shopItem.stock;
        }

        currentQuantity = 1;

        if (quantitySlider != null)
        {
            quantitySlider.minValue = 1;
            quantitySlider.maxValue = maxQuantity;
            quantitySlider.value = currentQuantity;
        }

        RefreshTransactionDetails();
    }

    void RefreshTransactionDetails()
    {
        if (selectedIndex < 0 || selectedIndex >= shopItems.Count || shopItems[selectedIndex].itemData == null)
        {
            ClearDetails();
            return;
        }

        var shopItem = shopItems[selectedIndex];

        if (txtItemName != null) txtItemName.text = shopItem.itemData.itemName;
        if (txtItemUsage != null) txtItemUsage.text = shopItem.itemData.itemUsageDescription;
        if (txtCount != null) txtCount.text = $"Quantity: {currentQuantity}";

        int totalPrice = shopItem.price * currentQuantity;
        if (txtPrice != null) txtPrice.text = totalPrice.ToString();

        if (isBuyMode && btnBuy != null)
        {
            btnBuy.gameObject.SetActive(true);
            btnBuy.interactable = (playerCoins >= totalPrice && currentQuantity <= shopItem.stock);
        }
        else if (!isBuyMode && btnSell != null)
        {
            btnSell.gameObject.SetActive(true);
            btnSell.interactable = (currentQuantity <= shopItem.stock);
        }
    }

    void OnAddButtonClicked()
    {
        if (selectedIndex < 0) return;
        if (currentQuantity < maxQuantity)
        {
            currentQuantity++;
            if (quantitySlider != null) quantitySlider.value = currentQuantity;
            RefreshTransactionDetails();
        }
    }

    void OnMinusButtonClicked()
    {
        if (selectedIndex < 0) return;
        if (currentQuantity > 1)
        {
            currentQuantity--;
            if (quantitySlider != null) quantitySlider.value = currentQuantity;
            RefreshTransactionDetails();
        }
    }

    void OnSliderValueChanged(float value)
    {
        if (selectedIndex < 0) return;
        currentQuantity = Mathf.Clamp(Mathf.RoundToInt(value), 1, maxQuantity);
        RefreshTransactionDetails();
    }

    // --- LOGIC MUA VẬT PHẨM ---
    public void OnBuyItem()
    {
        if (selectedIndex < 0 || selectedIndex >= shopItems.Count) return;
        var shopItem = shopItems[selectedIndex];
        int totalPrice = shopItem.price * currentQuantity;

        if (playerCoins < totalPrice)
        {
            Debug.Log("Không đủ tiền!");
            return;
        }

        bool addedSuccess = inventoryManager != null && inventoryManager.AddItem(shopItem.itemData, currentQuantity);
        if (!addedSuccess)
        {
            Debug.Log("Inventory đã đầy!");
            return;
        }

        playerCoins -= totalPrice;
        UpdateCoinUI();

        shopItem.stock -= currentQuantity;

        // Nếu mua hết sạch, tự động xóa, dồn danh sách và ẩn ô trống
        if (shopItem.stock <= 0)
        {
            shopItems.RemoveAt(selectedIndex);
            ClearDetails();
        }
        else
        {
            OnSelectShopSlot(selectedIndex);
        }

        CleanAndSortShopList();
        UpdateShopUI();
    }

    // --- LOGIC BÁN VẬT PHẨM ---
    public void OnSellItem()
    {
        if (selectedIndex < 0 || selectedIndex >= shopItems.Count) return;
        var shopItem = shopItems[selectedIndex];
        int totalEarnings = shopItem.price * currentQuantity;

        bool removedSuccess = RemoveItemFromInventory(shopItem.itemData, currentQuantity);
        if (!removedSuccess)
        {
            Debug.Log("Không đủ vật phẩm trong Inventory để bán!");
            return;
        }

        playerCoins += totalEarnings;
        UpdateCoinUI();

        shopItem.stock -= currentQuantity;

        // Nếu bán hết sạch, tự động xóa, dồn danh sách và ẩn ô trống
        if (shopItem.stock <= 0)
        {
            shopItems.RemoveAt(selectedIndex);
            ClearDetails();
        }
        else
        {
            OnSelectShopSlot(selectedIndex);
        }

        CleanAndSortShopList();
        UpdateShopUI();
    }

    bool RemoveItemFromInventory(ItemData itemToSell, int amountToRemove)
    {
        if (inventoryManager == null) return false;

        int remainingToRemove = amountToRemove;
        foreach (var slot in inventoryManager.slots)
        {
            if (slot.itemData == itemToSell && slot.count > 0)
            {
                if (slot.count >= remainingToRemove)
                {
                    slot.count -= remainingToRemove;
                    remainingToRemove = 0;
                    if (slot.count <= 0)
                    {
                        slot.itemData = null;
                        slot.count = 0;
                    }
                    break;
                }
                else
                {
                    remainingToRemove -= slot.count;
                    slot.itemData = null;
                    slot.count = 0;
                }
            }
        }

        inventoryManager.UpdateInventoryUI();
        return remainingToRemove == 0;
    }

    void UpdateCoinUI()
    {
        if (txtYourCoin != null) txtYourCoin.text = playerCoins.ToString();
    }

    void ClearDetails()
    {
        selectedIndex = -1;
        if (txtItemName != null) txtItemName.text = "";
        if (txtItemUsage != null) txtItemUsage.text = "";
        if (txtPrice != null) txtPrice.text = "0";
        if (txtCount != null) txtCount.text = "";
        if (quantitySlider != null) { quantitySlider.minValue = 1; quantitySlider.maxValue = 1; quantitySlider.value = 1; }
        if (btnBuy != null) btnBuy.gameObject.SetActive(false);
        if (btnSell != null) btnSell.gameObject.SetActive(false);
    }
}