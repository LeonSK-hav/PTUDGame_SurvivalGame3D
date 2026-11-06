using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Weapons/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("Basic Info")]
    public string weaponName = "New Weapon";
    public EquipmentType equipmentType = EquipmentType.Weapon;
    public Sprite weaponIcon;

    [Header("Level Requirement")]
    [Min(1)] public int requiredLevel = 1;

    [Header("Combat Stats")]
    public int damage = 25;
    [Min(0.01f)] public float fireRate = 0.5f;
    [Min(0f)] public float range = 100f;

    [Header("Magazine & Ammo")]
    [Min(1)] public int magazineSize = 8;
    [Min(0.1f)] public float reloadDuration = 1.2f;
    [Tooltip("Phân loại đạn sử dụng")]
    public AmmoType ammoType = AmmoType.PistolAmmo;
    [Tooltip("Vật phẩm đạn tương ứng trong Inventory")]
    public ItemData ammoTypeItem;

    [Header("Audio")]
    public AudioClip shootSound;
    public AudioClip reloadSound;

    [Header("Effects")]
    public ParticleSystem muzzleFlashPrefab;

    /// <summary>
    /// Tiện ích lấy AmmoType từ ammoTypeItem nếu được gán, hoặc lấy từ ammoType
    /// </summary>
    public AmmoType AmmoType
    {
        get
        {
            if (ammoTypeItem != null && ammoTypeItem.ammoType != AmmoType.None)
            {
                return ammoTypeItem.ammoType;
            }
            return ammoType;
        }
    }
}
