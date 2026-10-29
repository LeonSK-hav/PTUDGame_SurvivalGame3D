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
    public string itemUsageDescription; // Thông tin công dụng khi xuất ra
    
    [Header("Stats Effect")]
    public float healAmount;    // Hồi máu
    public float damageBonus;   // Sát thương cộng thêm
    
    [Header("Stack Settings")]
    public int maxStack = 99;   // Giới hạn tối đa trong kho là 99
}
