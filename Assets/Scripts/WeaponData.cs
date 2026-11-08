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

    [Header("Melee Settings (chỉ dùng khi isMelee = true)")]
    public bool isMelee = false;
    [Min(0.1f)] public float attackRadius = 1.2f;
    [Min(0.5f)] public float attackRange = 2.0f;
    [Min(0.05f)] public float swingCooldown = 0.8f;
    [Min(0f)] public float hitDelay = 0.2f;
    public AudioClip swingSound;
    public AudioClip hitSound;
    public ParticleSystem hitEffectPrefab;

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
            if (isMelee)
            {
                return AmmoType.None;
            }
            if (ammoTypeItem != null && ammoTypeItem.ammoType != AmmoType.None)
            {
                return ammoTypeItem.ammoType;
            }
            return ammoType;
        }
    }
}
