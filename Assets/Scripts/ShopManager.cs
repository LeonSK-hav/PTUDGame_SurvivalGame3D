using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
    public GameObject player;                 // Tham chiếu tới Player trong Scene (chứa PlayerStats)

    [Header("Pricing Settings")]
    public int defaultItemPrice = 20;         // Giá mặc định (mặc định là 20 nếu thiếu thông tin)

    [Header("Transaction UI")]
    public TextMeshProUGUI txtItemName;       // Tên vật phẩm đang chọn
    public TextMeshProUGUI txtItemUsage;      // Mô tả công dụng
    public TextMeshProUGUI txtPrice;          // Tổng giá tiền thanh toán
    public TextMeshProUGUI txtCount;          // Hiển thị dạng "Quantity: X"
    public TextMeshProUGUI txtYourCoin;       // Hiển thị số tiền của player (lấy từ PlayerStats)
    public TextMeshProUGUI txtYourCoinMain;       // Hiển thị số tiền của player (lấy từ PlayerStats)

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

    // Thuộc tính phụ trợ giúp lấy và gán tiền trực tiếp từ PlayerStats một cách an toàn
    private int PlayerCoins
    {
        get
        {
            if (player != null)
            {
                PlayerStats stats = player.GetComponent<PlayerStats>();
                if (stats != null) return stats.coins; // Giả định biến tiền trong PlayerStats tên là coins
            }
            return 0;
        }
        set
        {
            if (player != null)
            {
                PlayerStats stats = player.GetComponent<PlayerStats>();
                if (stats != null)
                {
                    stats.coins = value;
                    // Nếu PlayerStats có hàm cập nhật UI tiền riêng, có thể gọi ở đây
                }
            }
        }
    }

    void Start()
    {
        // Tự động tìm Player nếu chưa kéo vào Inspector
        if (player == null)
        {
            GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");
            if (foundPlayer != null) player = foundPlayer;
        }

        if (btnAdd != null) btnAdd.onClick.AddListener(OnAddButtonClicked);
        if (btnMinus != null) btnMinus.onClick.AddListener(OnMinusButtonClicked);

        if (quantitySlider != null)
        {
            quantitySlider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        if (btnBuy != null) btnBuy.onClick.AddListener(OnBuyItem);
        if (btnSell != null) btnSell.onClick.AddListener(OnSellItem);

        // Nếu là Shop Mua và danh sách trống, tự động load ngẫu nhiên các Item từ thư mục Assets/Items
        if (isBuyMode && (shopItems == null || shopItems.Count == 0))
        {
            LoadRandomItemsForBuyShop();
        }

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

    public void LoadRandomItemsForBuyShop()
    {
        shopItems.Clear();
#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/Items" });
        List<ItemData> allLoadedItems = new List<ItemData>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item != null)
            {
                allLoadedItems.Add(item);
            }
        }

        int maxSlots = slotsContainer != null ? slotsContainer.childCount : 10;
        
        // Trộn ngẫu nhiên danh sách item
        for (int i = 0; i < allLoadedItems.Count; i++)
        {
            ItemData temp = allLoadedItems[i];
            int randomIndex = Random.Range(i, allLoadedItems.Count);
            allLoadedItems[i] = allLoadedItems[randomIndex];
            allLoadedItems[randomIndex] = temp;
        }

        int itemsToAddCount = Mathf.Min(maxSlots, allLoadedItems.Count);
        for (int i = 0; i < itemsToAddCount; i++)
        {
            ItemData loadedItem = allLoadedItems[i];
            int randomStock=0;
            if(loadedItem.itemType == ItemType.Equipment) // Nếu là vật phẩm trang bị, số lượng tồn kho sẽ ít hơn
            {
                randomStock = 1; // Nếu không là vật phẩm tiêu hao, số lượng tồn kho sẽ nhiều hơn
            }
            else{
                
                randomStock = Random.Range(1, 99); // Nếu là vật phẩm tiêu hao, số lượng tồn kho sẽ nhiều hơn
            }

            shopItems.Add(new ShopItem
            {
                itemData = loadedItem,
                price = loadedItem.Price > 0 ? loadedItem.Price : defaultItemPrice, // Nếu item có giá > 0 thì dùng giá đó, nếu không thì dùng defaultItemPrice
                stock = randomStock
            });
        }
#else
        Debug.LogWarning("Load ngẫu nhiên từ thư mục Assets/Items chỉ hoạt động trong môi trường Unity Editor.");
#endif
    }

    public void CleanAndSortShopList()
    {
        shopItems.RemoveAll(item => item == null || item.itemData == null || item.stock <= 0);
    }

    public void SyncShopItemsFromInventory()
    {
        shopItems.Clear();
        if (inventoryManager == null) return;

        foreach (var slot in inventoryManager.slots)
        {
            if (slot.itemData != null && slot.count > 0)
            {
                var existingItem = shopItems.Find(x => x.itemData == slot.itemData);
                if (existingItem != null)
                {
                    existingItem.stock += slot.count;
                }
                else
                {
                    int basePrice = slot.itemData.Price > 0 ? slot.itemData.Price : 20;
                    int sellPrice =  basePrice * 2 / 3;

                    shopItems.Add(new ShopItem
                    {
                        itemData = slot.itemData,
                        price = sellPrice,
                        stock = slot.count
                    });
                }
            }
        }
        CleanAndSortShopList();
        UpdateShopUI();
    }

    public void UpdateShopUI()
    {
        if (slotsContainer == null) return;

        CleanAndSortShopList();

        for (int i = 0; i < slotsContainer.childCount; i++)
        {
            Transform slotTransform = slotsContainer.GetChild(i);
            Transform imgItemTransform = slotTransform.Find("imgItem");
            Transform txtCountTransform = slotTransform.Find("txtCount");
            Button slotButton = slotTransform.GetComponent<Button>();

            Image imgItem = imgItemTransform != null ? imgItemTransform.GetComponent<Image>() : null;
            TextMeshProUGUI countText = txtCountTransform != null ? txtCountTransform.GetComponent<TextMeshProUGUI>() : null;

            if (i < shopItems.Count && shopItems[i].itemData != null && shopItems[i].stock > 0)
            {
                slotTransform.gameObject.SetActive(true);
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

                int index = i;
                if (slotButton != null)
                {
                    slotButton.onClick.RemoveAllListeners();
                    slotButton.onClick.AddListener(() => OnSelectShopSlot(index));
                }
            }
            else
            {
                slotTransform.gameObject.SetActive(false);
            }
        }
    }

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
            int affordableCount = shopItem.price > 0 ? PlayerCoins / shopItem.price : 99;
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
        int unitPrice=0;
        if(isBuyMode)
        {
             unitPrice = shopItem.price;

        }
        {
            unitPrice = shopItem.price * 2 / 3;
        }
        int totalPrice = unitPrice * currentQuantity;

        if (txtPrice != null)
        {
            txtPrice.text = totalPrice.ToString();
        }

        if (isBuyMode && btnBuy != null)
        {
            btnBuy.gameObject.SetActive(true);
            btnBuy.interactable = (PlayerCoins >= totalPrice && currentQuantity <= shopItem.stock);
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

    void OnEnable()
    {
        if (player == null)
        {
            GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");
            if (foundPlayer != null) player = foundPlayer;
        }

        if (!isBuyMode && inventoryManager != null)
        {
            SyncShopItemsFromInventory();
        }
        else
        {
            LoadRandomItemsForBuyShop();
            CleanAndSortShopList();
            UpdateShopUI();
        }
        UpdateCoinUI();
        ClearDetails();
    }

    public void OnBuyItem()
    {
        if (selectedIndex < 0 || selectedIndex >= shopItems.Count) return;
        var shopItem = shopItems[selectedIndex];
        int totalPrice = shopItem.price * currentQuantity;

        if (PlayerCoins < totalPrice)
        {
            Debug.Log("Không đủ tiền!");
        }
        else
        {
                bool addedSuccess = inventoryManager != null && inventoryManager.AddItem(shopItem.itemData, currentQuantity);
            if (!addedSuccess)
            {
                Debug.Log("Inventory đã đầy!");
                return;
            }

            // Trừ trực tiếp tiền vào PlayerStats thông qua thuộc tính chung
            PlayerCoins -= totalPrice;
            UpdateCoinUI();

            shopItem.stock -= currentQuantity;

            if (shopItem.stock <= 0)
            {
                shopItems.RemoveAt(selectedIndex);
                ClearDetails();
            }
            else
            {
                OnSelectShopSlot(selectedIndex);
            }
        }

        

        CleanAndSortShopList();
        UpdateShopUI();
    }

    public void OnSellItem()
    {
        if (selectedIndex < 0 || selectedIndex >= shopItems.Count) return;
        var shopItem = shopItems[selectedIndex];
        int totalEarnings = shopItem.price * 2 / 3 * currentQuantity;

        bool removedSuccess = RemoveItemFromInventory(shopItem.itemData, currentQuantity);
        if (!removedSuccess)
        {
            Debug.Log("Không đủ vật phẩm trong Inventory để bán!");
            return;
        }

        // Cộng trực tiếp tiền vào PlayerStats thông qua thuộc tính chung
        PlayerCoins += totalEarnings;
        UpdateCoinUI();

        shopItem.stock -= currentQuantity;

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
        if (txtYourCoin != null) txtYourCoin.text = PlayerCoins.ToString();
        if (txtYourCoinMain != null) txtYourCoinMain.text = PlayerCoins.ToString();
    }

    void ClearDetails()
    {
        selectedIndex = -1;
        if (txtItemName != null) txtItemName.text = "";
        if        (txtItemUsage != null) txtItemUsage.text = "";
        if (txtPrice != null) txtPrice.text = "0";
        if (txtCount != null) txtCount.text = "";
        if (quantitySlider != null) { quantitySlider.minValue = 1; quantitySlider.maxValue = 1; quantitySlider.value = 1; }
        if (btnBuy != null) btnBuy.gameObject.SetActive(false);
        if (btnSell != null) btnSell.gameObject.SetActive(false);
    }
}