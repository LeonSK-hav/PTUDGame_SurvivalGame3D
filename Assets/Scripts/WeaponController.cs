using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    private static readonly int AimStateHash = Animator.StringToHash("Base Layer.Aim");
    private static readonly int AimShootStateHash = Animator.StringToHash("Base Layer.Aim Shoot");
    private static readonly int IdleStateHash = Animator.StringToHash("Base Layer.Idle");
    private static readonly int WalkStateHash = Animator.StringToHash("Base Layer.Walk");
    private static readonly int RunStateHash = Animator.StringToHash("Base Layer.Run");

    [Header("Weapon Configuration")]
    public WeaponData weaponData;

    [Header("Combat Stats (Fallback or Overridden by WeaponData)")]
    public float BulletRange = 100f;
    public float FireRate = 0.5f;
    public int BulletsDamage = 25;
    public int BulletsInMag = 8;
    public int currentBullets = 0;
    public float reloadDuration = 1.2f;

    [Header("Visual & Audio")]
    public ParticleSystem muzzleFlash;
    public AudioClip shootSound;
    public AudioClip reloadSound;

    [Header("Raycast & Camera")]
    public Transform playerCamera;
    public LayerMask shootingMask = ~0;

    private Animator animator;
    private PlayerController player;
    private float nextFireTime = 0f;
    private bool isReloading = false;
    private Coroutine reloadCoroutine;
    private Coroutine aimShootCoroutine;
    private bool isAiming;
    private bool isPlayingAimShoot;
    private bool aimRequiresRelease;

    public int CurrentBullets
    {
        get { return currentBullets; }
    }

    public int MaxBulletsInMag
    {
        get { return BulletsInMag; }
    }

    public bool IsReloading
    {
        get { return isReloading; }
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        ResolvePlayer();
        ResolveCamera();
    }

    private void Start()
    {
        if (weaponData != null)
        {
            SetupWeapon(weaponData);
        }
        else
        {
            currentBullets = BulletsInMag;
        }
    }

    private void OnDisable()
    {
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }
        if (aimShootCoroutine != null)
        {
            StopCoroutine(aimShootCoroutine);
            aimShootCoroutine = null;
        }
        PlayMovementState();
        isReloading = false;
        isAiming = false;
        isPlayingAimShoot = false;
    }

    [Header("Source Item")]
    [Tooltip("Vật phẩm ItemData tương ứng khi trang bị từ Inventory")]
    public ItemData sourceItemData;

    /// <summary>
    /// Khởi tạo hoặc cập nhật cấu hình súng từ ScriptableObject WeaponData
    /// </summary>
    public void SetupWeapon(WeaponData data, ItemData item = null)
    {
        weaponData = data;
        if (item != null)
        {
            sourceItemData = item;
        }

        if (weaponData == null) return;

        BulletRange = weaponData.range;
        FireRate = weaponData.fireRate;
        BulletsDamage = weaponData.damage;
        BulletsInMag = weaponData.magazineSize;
        currentBullets = BulletsInMag;

        if (weaponData.reloadDuration > 0f)
        {
            reloadDuration = weaponData.reloadDuration;
        }

        if (weaponData.shootSound != null)
        {
            shootSound = weaponData.shootSound;
        }

        if (weaponData.reloadSound != null)
        {
            reloadSound = weaponData.reloadSound;
        }

        if (muzzleFlash == null && weaponData.muzzleFlashPrefab != null)
        {
            muzzleFlash = Instantiate(weaponData.muzzleFlashPrefab, transform);
        }
    }

    /// <summary>
    /// Lấy ItemData của vũ khí này (dùng để trả lại túi đồ khi tháo trang bị)
    /// </summary>
    public ItemData GetSourceItemData()
    {
        if (sourceItemData != null) return sourceItemData;

        string gName = weaponData != null ? weaponData.weaponName : name;
        // Loại bỏ hậu tố (Clone) hoặc GUID nếu có
        if (gName.Contains("_")) gName = gName.Split('_')[0];
        if (gName.Contains("(")) gName = gName.Split('(')[0].Trim();

        ItemData loaded = Resources.Load<ItemData>($"Items/{gName}");
#if UNITY_EDITOR
        if (loaded == null)
        {
            loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>($"Assets/Items/{gName}.asset");
        }
#endif
        return loaded;
    }

    private void ResolvePlayer()
    {
        if (player != null) return;

        player = GetComponentInParent<PlayerController>();
        if (player == null && transform.parent != null)
        {
            player = transform.parent.root.GetComponent<PlayerController>();
        }
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
        }
    }

    private void ResolveCamera()
    {
        if (playerCamera != null) return;

        if (Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }
        else
        {
            Camera cam = GetComponentInParent<Camera>();
            if (cam != null)
            {
                playerCamera = cam.transform;
            }
        }
    }

    private void Update()
    {
        // Chỉ vũ khí đang được kích hoạt trên tay người chơi mới được nhận Input
        if (WeaponSwitcher.Instance != null && WeaponSwitcher.Instance.GetActiveWeapon() != this)
        {
            SetAiming(false);
            return;
        }

        bool gameplayInputBlocked =
            (UIManager.Instance != null && UIManager.Instance.IsGameplayInputBlocked) ||
            Cursor.visible || CursorLockMode.Locked != Cursor.lockState;

        bool aimHeld = false;
        bool shootHeld = false;
        if (!gameplayInputBlocked && InputManager.Instance != null && InputManager.Instance.playerInput != null)
        {
            aimHeld = Mouse.current != null && Mouse.current.rightButton.isPressed;
            shootHeld = InputManager.Instance.playerInput.Player.Fire.IsPressed();
        }

        if (!aimHeld)
        {
            aimRequiresRelease = false;
        }

        bool isRunning = player != null && player.IsRunning;
        bool canAim = aimHeld && !aimRequiresRelease && !isRunning && !isReloading;
        SetAiming(canAim);

        // Chuột phải giữ để ngắm; chuột trái tiếp tục bắn theo nhịp như cơ chế cũ.
        if (!gameplayInputBlocked && shootHeld && !isRunning && !isReloading &&
            Time.time >= nextFireTime && currentBullets > 0)
        {
            bool forceReleaseAfterShot = isAiming && RequiresAimReleaseAfterShot();
            Shoot(isAiming);
            nextFireTime = Time.time + FireRate;

            if (forceReleaseAfterShot)
            {
                aimRequiresRelease = true;
                SetAiming(false);
            }
        }

        // Xử lý nạp đạn (phím R)
        if (!gameplayInputBlocked && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            Reload();
        }

        // Đồng bộ animation
        if (animator != null && player != null)
        {
            animator.SetBool("Walk", player.IsWalking);
            animator.SetBool("Run", player.IsRunning);
        }
    }

    private void SetAiming(bool aiming)
    {
        if (isAiming == aiming) return;

        isAiming = aiming;
        if (animator == null || isPlayingAimShoot) return;

        if (aiming)
        {
            PlayAnimatorState(AimStateHash);
        }
        else
        {
            PlayMovementState();
        }
    }

    private bool RequiresAimReleaseAfterShot()
    {
        AmmoType ammoType = weaponData != null && weaponData.AmmoType != AmmoType.None
            ? weaponData.AmmoType
            : fallbackAmmoType;
        return ammoType == AmmoType.ShotgunAmmo || ammoType == AmmoType.SniperAmmo;
    }

    private void PlayMovementState()
    {
        if (animator == null) return;

        int stateHash = player != null && player.IsRunning
            ? RunStateHash
            : player != null && player.IsWalking
                ? WalkStateHash
                : IdleStateHash;
        PlayAnimatorState(stateHash);
    }

    private void PlayAnimatorState(int stateHash)
    {
        if (animator != null && animator.HasState(0, stateHash))
        {
            animator.Play(stateHash, 0, 0f);
        }
    }

    private IEnumerator ReturnFromAimShoot()
    {
        yield return null;

        if (animator != null && animator.HasState(0, AimShootStateHash))
        {
            float animationDuration = animator.GetCurrentAnimatorStateInfo(0).length;
            if (animationDuration > 0f)
            {
                yield return new WaitForSeconds(animationDuration);
            }
        }

        isPlayingAimShoot = false;
        if (animator != null)
        {
            if (isAiming)
            {
                PlayAnimatorState(AimStateHash);
            }
            else
            {
                PlayMovementState();
            }
        }

        aimShootCoroutine = null;
    }

    [Header("Ammo Settings (Dự phòng khi chưa gán WeaponData)")]
    public AmmoType fallbackAmmoType = AmmoType.PistolAmmo;

    public void Reload()
    {
        if (isReloading || currentBullets >= BulletsInMag) return;

        int neededAmmo = BulletsInMag - currentBullets;
        if (neededAmmo <= 0) return;

        ItemData ammoItem = weaponData != null ? weaponData.ammoTypeItem : null;
        AmmoType aType = (weaponData != null && weaponData.AmmoType != AmmoType.None) ? weaponData.AmmoType : fallbackAmmoType;

        if (ammoItem == null && aType == AmmoType.None)
        {
            // Chế độ fallback: Không có cấu hình loại đạn -> Nạp đầy vô hạn
            if (reloadCoroutine != null)
            {
                StopCoroutine(reloadCoroutine);
            }
            reloadCoroutine = StartCoroutine(ReloadRoutine(neededAmmo, false));
            return;
        }

        // Chế độ thực tế: Kiểm tra đạn trong Inventory
        int availableAmmoInInventory = GetTotalReservedAmmo();

        if (availableAmmoInInventory <= 0)
        {
            string gunName = weaponData != null ? weaponData.weaponName : name;
            Debug.LogWarning($"<color=red><b>[WeaponController] Hết đạn dự trữ trong kho đồ cho {gunName}! (Cần {neededAmmo} viên)</b></color>");
            return;
        }

        int actualAmmoToReload = Mathf.Min(neededAmmo, availableAmmoInInventory);

        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
        }
        reloadCoroutine = StartCoroutine(ReloadRoutine(actualAmmoToReload, true));
    }

    private IEnumerator ReloadRoutine(int ammoToReload, bool consumeFromInventory)
    {
        isReloading = true;

        if (animator != null)
        {
            animator.SetTrigger("Reload");
        }

        if (AudioManager.Instance != null && reloadSound != null)
        {
            AudioManager.Instance.PlayEffectSound(reloadSound);
        }

        float duration = Mathf.Max(0.1f, reloadDuration);
        yield return new WaitForSeconds(duration);

        if (consumeFromInventory)
        {
            InventoryManager inv = InventoryManager.Instance != null ? InventoryManager.Instance : FindFirstObjectByType<InventoryManager>();
            if (inv != null)
            {
                ItemData ammoItem = weaponData != null ? weaponData.ammoTypeItem : null;
                AmmoType aType = (weaponData != null && weaponData.AmmoType != AmmoType.None) ? weaponData.AmmoType : fallbackAmmoType;
                inv.RemoveAmmo(ammoItem, aType, ammoToReload);
            }
        }

        currentBullets = Mathf.Min(BulletsInMag, currentBullets + ammoToReload);
        isReloading = false;
        reloadCoroutine = null;

        string weaponTitle = weaponData != null ? weaponData.weaponName : name;
        Debug.Log($"<color=yellow>[WeaponController] Đã nạp {ammoToReload} viên đạn cho {weaponTitle}. Băng đạn: {currentBullets}/{BulletsInMag}. Dự trữ trong túi: {GetTotalReservedAmmo()}</color>");
    }

    /// <summary>
    /// Trả về tổng số đạn dự trữ trong Inventory của loại đạn mà súng này sử dụng.
    /// Trả về -1 nếu súng ở chế độ fallback không giới hạn.
    /// </summary>
    public int GetTotalReservedAmmo()
    {
        InventoryManager inv = InventoryManager.Instance != null ? InventoryManager.Instance : FindFirstObjectByType<InventoryManager>();
        if (inv == null) return 0;

        ItemData ammoItem = weaponData != null ? weaponData.ammoTypeItem : null;
        AmmoType aType = (weaponData != null && weaponData.AmmoType != AmmoType.None) ? weaponData.AmmoType : fallbackAmmoType;

        if (ammoItem == null && aType == AmmoType.None)
        {
            return -1; // Vô hạn / Fallback
        }

        int total = 0;
        if (ammoItem != null)
        {
            total = inv.GetItemCount(ammoItem);
        }
        if (total == 0 && aType != AmmoType.None)
        {
            total = inv.GetAmmoCount(aType);
        }

        return total;
    }

    public void Shoot()
    {
        Shoot(false);
    }

    private void Shoot(bool aimed)
    {
        if (currentBullets <= 0) return;

        currentBullets--;

        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        if (animator != null)
        {
            if (aimed && animator.HasState(0, AimShootStateHash))
            {
                if (aimShootCoroutine != null)
                {
                    StopCoroutine(aimShootCoroutine);
                }

                isPlayingAimShoot = true;
                animator.Play(AimShootStateHash, 0, 0f);
                aimShootCoroutine = StartCoroutine(ReturnFromAimShoot());
            }
            else
            {
                animator.SetTrigger("Shoot");
            }
        }

        if (AudioManager.Instance != null && shootSound != null)
        {
            AudioManager.Instance.PlayEffectSound(shootSound);
        }

        ResolveCamera();
        if (playerCamera == null)
        {
            Debug.LogError("WeaponController chưa xác định được playerCamera để bắn raycast.", this);
            return;
        }

        RaycastHit hit;
        if (Physics.Raycast(playerCamera.position, playerCamera.forward, out hit, BulletRange, shootingMask))
        {
            Target t = hit.collider.GetComponent<Target>() ?? hit.collider.GetComponentInParent<Target>();
            ZombieAI z = hit.collider.GetComponent<ZombieAI>() ?? hit.collider.GetComponentInParent<ZombieAI>();

            if (t != null)
            {
                t.OnHit();
            }

            if (z != null)
            {
                z.TakeDamage(BulletsDamage);
            }
        }
    }
}
