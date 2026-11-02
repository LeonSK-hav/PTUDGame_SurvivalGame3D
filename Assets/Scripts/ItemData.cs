using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType { Consumable, Permanent, Equipment }

// Phân loại trang bị chi tiết
public enum EquipmentType { None, Weapon, Helmet, Armor, Pants, Boots, Accessories }

// Phân loại loại đạn hỗ trợ (có thể mở rộng thêm tùy ý)
public enum AmmoType { None, PistolAmmo, RifleAmmo, ShotgunAmmo, SniperAmmo }

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite itemIcon;
    public ItemType itemType; // Consumable (đồ ăn, thuốc, đạn), Permanent (súng, dao dùng vĩnh cửu), Equipment (quần áo, mũ)
    
    [Header("Equipment & Usage Requirements")]
    public EquipmentType equipmentType; // Phân loại trang bị chi tiết (Vũ khí, Mũ, Áo, Quần, Giày,...)
    public int requiredLevel = 1;       // Cấp độ tối thiểu yêu cầu để sử dụng trang bị/vật phẩm này

    [Header("Ammo Settings (Dùng cho Súng hoặc Đạn)")]
    public AmmoType ammoType;           // Loại đạn mà súng sử dụng HOẶC loại đạn của chính item đạn đó

    [Header("World Prefab")]
    public GameObject worldItemPrefab; // Prefab hiển thị dưới mặt đất khi vứt vật phẩm này ra thế giới

    [Header("Stats Effect")]
    public float healAmount;    // Hồi máu
    public float damageBonus;   // Sát thương cộng thêm
    public float defenseBonus;  // Phòng thủ cộng thêm
    public float agilityBonus;  // Nhanh nhẹn cộng thêm

    [TextArea]
    public string itemUsageDescription
    {
        get
        {
            string desc = "";

            // Hiển thị level yêu cầu nếu lớn hơn 1
            if (requiredLevel > 1)
            {
                desc += $"Level: Lv.{requiredLevel}\n";
            }

            // Hiển thị loại trang bị chi tiết lên mô tả nếu là trang bị
            if (itemType == ItemType.Equipment && equipmentType != EquipmentType.None)
            {
                desc += $"Equipment type: {GetEquipmentTypeName(equipmentType)}\n";
            }

            // Hiển thị thông tin loại đạn nếu có liên quan tới đạn/súng
            if (ammoType != AmmoType.None)
            {
                desc += $"Ammo type: {GetAmmoTypeName(ammoType)}\n";
            }

            if (healAmount > 0)
                desc += $"+{healAmount} Heal Amount\n";//hồi máu
            if (damageBonus > 0)
                desc += $"+{damageBonus} Damage\n";//sát thương
            if (defenseBonus > 0)
                desc += $"+{defenseBonus} Defense\n";//phòng ngự
            if (agilityBonus > 0)
                desc += $"+{agilityBonus} Agility\n";//nhanh nhẹn

            // Xóa ký tự xuống dòng thừa ở cuối nếu có
            return desc.TrimEnd('\n');
        }
    }

    [Header("Stack Settings")]
    public int maxStack = 99;   // Giới hạn tối đa trong kho là 99

    // Hàm hỗ trợ chuyển tên Enum EquipmentType sang tiếng Việt hiển thị trên UI mô tả
    private string GetEquipmentTypeName(EquipmentType eqType)
    {
        switch (eqType)
        {
            case EquipmentType.Weapon: return "Weapon";//Vũ khí
            case EquipmentType.Helmet: return "Helmet";//Mũ
            case EquipmentType.Armor: return "Armor";//Áo giáp
            case EquipmentType.Pants: return "Pants";
            case EquipmentType.Boots: return "Boots";//Giày
            case EquipmentType.Accessories: return "Phụ kiện";
            default: return "Khác";
        }
    }

    // Hàm hỗ trợ chuyển tên Enum AmmoType sang tiếng Việt hiển thị trên UI mô tả
    private string GetAmmoTypeName(AmmoType aType)
    {
        switch (aType)
        {
            case AmmoType.PistolAmmo: return "Pistol Ammo";//Đạn Súng Lục
            case AmmoType.RifleAmmo: return "Rifle Ammo";//Đạn Súng Trường / AK
            case AmmoType.ShotgunAmmo: return "Shotgun Ammo";//Đạn Shotgun
            case AmmoType.SniperAmmo: return "Sniper Ammo";//Đạn Súng Ngắm
            default: return "Standard Ammo";//Đạn Thường
        }
    }
}