using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    [Header("Weapon Configuration")]
    public WeaponData weaponData;

    [Header("Combat Stats (Fallback or Overridden by WeaponData)")]
    public float BulletRange = 100f;
    public float FireRate = 0.5f;
    public int BulletsDamage = 25;
    public int BulletsInMag = 8;
    public int currentBullets = 0;

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

    public int CurrentBullets
    {
        get { return currentBullets; }
    }

    public int MaxBulletsInMag
    {
        get { return BulletsInMag; }
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

    private void OnEnable()
    {
        ResolvePlayer();
        ResolveCamera();
    }

    /// <summary>
    /// Khởi tạo hoặc cập nhật cấu hình súng từ ScriptableObject WeaponData
    /// </summary>
    public void SetupWeapon(WeaponData data)
    {
        weaponData = data;
        if (weaponData == null) return;

        BulletRange = weaponData.range;
        FireRate = weaponData.fireRate;
        BulletsDamage = weaponData.damage;
        BulletsInMag = weaponData.magazineSize;
        currentBullets = BulletsInMag;

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
        bool gameplayInputBlocked =
            (UIManager.Instance != null && UIManager.Instance.IsGameplayInputBlocked) ||
            Cursor.visible || CursorLockMode.Locked != Cursor.lockState;

        // Xử lý bắn
        if (!gameplayInputBlocked && InputManager.Instance != null && InputManager.Instance.playerInput != null)
        {
            var fireAction = InputManager.Instance.playerInput.Player.Fire;
            if (fireAction.WasPressedThisFrame() || fireAction.IsPressed())
            {
                bool isRunning = player != null && player.IsRunning;
                if (!isRunning)
                {
                    if (Time.time >= nextFireTime && currentBullets > 0)
                    {
                        Shoot();
                        nextFireTime = Time.time + FireRate;
                    }
                }
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

    public void Reload()
    {
        if (currentBullets >= BulletsInMag) return;

        currentBullets = BulletsInMag;

        if (AudioManager.Instance != null && reloadSound != null)
        {
            AudioManager.Instance.PlayEffectSound(reloadSound);
        }
    }

    public void Shoot()
    {
        if (currentBullets <= 0) return;

        currentBullets--;

        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        if (animator != null)
        {
            animator.SetTrigger("Shoot");
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
