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
        isReloading = false;
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

        // Xử lý bắn (chặn khi đang chạy hoặc đang nạp đạn)
        if (!gameplayInputBlocked && InputManager.Instance != null && InputManager.Instance.playerInput != null)
        {
            var fireAction = InputManager.Instance.playerInput.Player.Fire;
            if (fireAction.WasPressedThisFrame() || fireAction.IsPressed())
            {
                bool isRunning = player != null && player.IsRunning;
                if (!isRunning && !isReloading)
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
        if (isReloading || currentBullets >= BulletsInMag) return;

        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
        }
        reloadCoroutine = StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
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

        currentBullets = BulletsInMag;
        isReloading = false;
        reloadCoroutine = null;
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
