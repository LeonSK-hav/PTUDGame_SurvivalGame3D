using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopUIManager : MonoBehaviour
{
    [Header("Shop Panels")]
    public GameObject shopMainCanvas;   // Cửa sổ tổng của Shop (Panel bọc ngoài cùng)
    public GameObject buyPanel;         // Panel giao diện Mua
    public GameObject sellPanel;        // Panel giao diện Bán

    [Header("References")]
    public ShopManager buyShopManager;  // Script ShopManager nằm ở Panel Mua
    public ShopManager sellShopManager; // Script ShopManager nằm ở Panel Bán

    void Start()
    {
        // Đảm bảo ban đầu shop đóng lại
        if (shopMainCanvas != null)
        {
            shopMainCanvas.SetActive(false);
        }
    }

    // --- HÀM GỌI KHI MỞ SHOP (Gắn vào nút Mở Shop ngoài game) ---
    public void OpenShop()
    {
        if (shopMainCanvas != null)
        {
            shopMainCanvas.SetActive(true);
        }

        // Mặc định hiển thị giao diện Mua trước tiên
       ShowBuyPanel();
    }

    // --- HÀM GỌI KHI ĐÓNG SHOP (Gắn vào nút Close/Exit Shop) ---
    public void CloseShop()
    {
        if (shopMainCanvas != null)
        {
            shopMainCanvas.SetActive(false);
        }
    }

    // --- CHUYỂN SANG GIAO DIỆN MUA ---
    public void ShowBuyPanel()
    {
        if (buyPanel != null) buyPanel.SetActive(true);
        if (sellPanel != null) sellPanel.SetActive(false);

        // Kích hoạt dữ liệu shop mua nếu cần
        if (buyShopManager != null)
        {
            buyShopManager.enabled = true;
        }
    }

    // --- CHUYỂN SANG GIAO DIỆN BÁN ---
    public void ShowSellPanel()
    {
        if (buyPanel != null) buyPanel.SetActive(false);
        if (sellPanel != null) sellPanel.SetActive(true);

        // Bật script Shop Bán và ÉP buộc đồng bộ ngay lập tức dữ liệu từ Inventory qua
        if (sellShopManager != null)
        {
            sellShopManager.enabled = true;
            sellShopManager.SyncShopItemsFromInventory(); // Quét sạch balo đưa vào shop bán
        }
    }
}