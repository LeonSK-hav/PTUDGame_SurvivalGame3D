using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType { Consumable, Permanent, Equipment }

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite itemIcon;
    public ItemType itemType; // Consumable (đồ ăn, thuốc, đạn), Permanent (súng, dao dùng vĩnh cửu), Equipment (quần áo, mũ)
    [TextArea]
    
    [Header("Stats Effect")]
    public float healAmount;    // Hồi máu
    public float damageBonus;   // Sát thương cộng thêm
    public float defenseBonus;  // Phòng ngự mới bổ sung
    public float agilityBonus;  // Nhanh nhẹn mới bổ sung
    [Header("Stack Settings")]
    public int maxStack = 99;   // Giới hạn tối đa trong kho là 99
    // Thuộc tính tự động sinh ra chuỗi hiển thị công dụng dựa trên các chỉ số lớn hơn 0
    public string itemUsageDescription
    {
        get
        {
            string desc = "";

            if (healAmount > 0)
                desc += $"+{healAmount} Hồi máu\n";
            if (damageBonus > 0)
                desc += $"+{damageBonus} Sát thương\n";
            if (defenseBonus > 0)
                desc += $"+{defenseBonus} Phòng ngự\n";
            if (agilityBonus > 0)
                desc += $"+{agilityBonus} Nhanh nhẹn\n";

            // Xóa ký tự xuống dòng thừa ở cuối nếu có
            return desc.TrimEnd('\n');
        }
    }
}
