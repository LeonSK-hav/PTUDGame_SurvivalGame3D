using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Quản lý cơ chế hoạt động, tấn công và va chạm của Vũ khí Cận chiến (Melee Weapon).
/// Kế thừa từ WeaponController để tương thích 100% với WeaponSwitcher và WeaponHUD.
/// Mỗi loại vũ khí cận chiến có Animator và AnimatorController riêng biệt tương tự hệ thống súng.
/// </summary>
public class MeleeController : WeaponController
{
    [Header("=== MELEE COMBAT SETTINGS ===")]
    [Tooltip("Sát thương cơ bản của đòn đánh (sẽ được ghi đè bởi WeaponData nếu có)")]
    public int attackDamage = 30;

    [Tooltip("Tầm với tối đa của đòn chém (mét)")]
    [Min(0.5f)] public float attackRange = 2.0f;

    [Tooltip("Bán kính vùng quét va chạm hình cầu (mét)")]
    [Min(0.1f)] public float attackRadius = 1.2f;

    [Tooltip("Thời gian hồi giữa 2 lần vung vũ khí (giây)")]
    [Min(0.05f)] public float swingCooldown = 0.8f;

    [Tooltip("Độ trễ từ khi bấm vung đến khi đòn đánh chạm mục tiêu (áp dụng khi không dùng Animation Event)")]
    [Min(0f)] public float hitDelay = 0.2f;

    [Tooltip("LayerMask quét mục tiêu trúng đòn (Zombie, Target, v.v.)")]
    public LayerMask hitMask = ~0;

    [Header("=== MELEE AUDIO & FX ===")]
    [Tooltip("Âm thanh vung gió khi chém")]
    public AudioClip swingSound;

    [Tooltip("Âm thanh khi chém trúng mục tiêu")]
    public AudioClip hitSound;

    [Tooltip("Hiệu ứng hạt tóe ra tại điểm chạm mục tiêu")]
    public ParticleSystem hitEffectPrefab;

    [Header("=== ANIMATION EVENT SETTINGS ===")]
    [Tooltip("Nếu bật true, sát thương sẽ chỉ gây ra khi Animation Event OnAttackHitAnimationEvent() được gọi")]
    public bool useAnimationEvent = false;

    // Trạng thái nội bộ
    private float nextAttackTime = 0f;
    private bool isAttacking = false;
    private Coroutine attackCoroutine;
    private PlayerStats playerStats;
    private AudioSource audioSource;

    /// <summary>
    /// Đánh dấu đây là vũ khí cận chiến để WeaponHUD và WeaponSwitcher nhận diện
    /// </summary>
    public override bool IsMeleeWeapon => true;

    public bool IsAttacking => isAttacking;

    protected override void Awake()
    {
        base.Awake();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = GetComponentInChildren<AudioSource>();
        }

        ResolvePlayerStats();

        // Vũ khí cận chiến không dùng đạn
        currentBullets = 0;
        BulletsInMag = 0;
    }

    protected override void Start()
    {
        if (weaponData != null)
        {
            SetupWeapon(weaponData, sourceItemData);
        }
        else
        {
            currentBullets = 0;
            BulletsInMag = 0;
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        isAttacking = false;
    }

    private void ResolvePlayerStats()
    {
        if (playerStats != null) return;

        if (player != null)
        {
            playerStats = player.GetComponent<PlayerStats>();
        }
        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<PlayerStats>();
        }
    }

    /// <summary>
    /// Nạp cấu hình từ WeaponData (ScriptableObject)
    /// </summary>
    public override void SetupWeapon(WeaponData data, ItemData item = null)
    {
        base.SetupWeapon(data, item);

        if (weaponData == null) return;

        attackDamage = weaponData.damage;

        if (weaponData.attackRange > 0f) attackRange = weaponData.attackRange;
        if (weaponData.attackRadius > 0f) attackRadius = weaponData.attackRadius;
        if (weaponData.swingCooldown > 0f) swingCooldown = weaponData.swingCooldown;
        if (weaponData.hitDelay >= 0f) hitDelay = weaponData.hitDelay;

        if (weaponData.swingSound != null) swingSound = weaponData.swingSound;
        if (weaponData.hitSound != null) hitSound = weaponData.hitSound;
        if (weaponData.hitEffectPrefab != null) hitEffectPrefab = weaponData.hitEffectPrefab;

        currentBullets = 0;
        BulletsInMag = 0;
    }

    protected override void Update()
    {
        // 1. Chỉ vũ khí đang được kích hoạt trên tay người chơi mới nhận Input
        if (WeaponSwitcher.Instance != null && WeaponSwitcher.Instance.GetActiveWeapon() != this)
        {
            return;
        }

        // 2. Kiểm tra trạng thái UI / Cursor
        bool gameplayInputBlocked =
            (UIManager.Instance != null && UIManager.Instance.IsGameplayInputBlocked) ||
            Cursor.visible || CursorLockMode.Locked != Cursor.lockState;

        // 3. Đồng bộ chuyển động Walk/Run cho Animator riêng của vũ khí cận chiến
        if (animator != null && player != null)
        {
            animator.SetBool("Walk", player.IsWalking);
            animator.SetBool("Run", player.IsRunning);
        }

        if (gameplayInputBlocked) return;

        // 4. Đọc input tấn công (Chuột trái / Fire action)
        bool attackPressed = false;
        if (InputManager.Instance != null && InputManager.Instance.playerInput != null)
        {
            attackPressed = InputManager.Instance.playerInput.Player.Fire.WasPressedThisFrame();
        }
        else if (Mouse.current != null)
        {
            attackPressed = Mouse.current.leftButton.wasPressedThisFrame;
        }

        // 5. Kiểm tra điều kiện thực hiện đòn tấn công:
        //    - Không thể tấn công khi đang Sprint (chạy nhanh)
        //    - Không thể tấn công chồng khi đang trong nhịp vung
        //    - Đã hết thời gian hồi chiêu (swingCooldown)
        bool isRunning = player != null && player.IsRunning;
        if (attackPressed && !isRunning && !isAttacking && Time.time >= nextAttackTime)
        {
            PerformAttack();
        }
    }

    /// <summary>
    /// Kích hoạt chuỗi đòn đánh cận chiến
    /// </summary>
    public void PerformAttack()
    {
        isAttacking = true;
        nextAttackTime = Time.time + swingCooldown;

        // Phát animation tấn công trên Animator riêng của vũ khí này
        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.SetTrigger("Attack");
        }

        // Phát âm thanh vung gió
        PlaySound(swingSound);

        // Nếu không dùng Animation Event trên clip, chạy coroutine tính trễ va chạm
        if (!useAnimationEvent)
        {
            if (attackCoroutine != null)
            {
                StopCoroutine(attackCoroutine);
            }
            attackCoroutine = StartCoroutine(AttackRoutine());
        }
    }

    private IEnumerator AttackRoutine()
    {
        if (hitDelay > 0f)
        {
            yield return new WaitForSeconds(hitDelay);
        }

        ExecuteHitCheck();

        // Chờ thời gian hoàn tất đòn đánh
        float remainingTime = Mathf.Max(0.05f, swingCooldown - hitDelay);
        yield return new WaitForSeconds(remainingTime);

        isAttacking = false;
        attackCoroutine = null;
    }

    /// <summary>
    /// Phương thức hỗ trợ gọi từ Animation Event trên clip Attack trong Unity
    /// </summary>
    public void OnAttackHitAnimationEvent()
    {
        ExecuteHitCheck();
    }

    /// <summary>
    /// Phương thức hỗ trợ gọi từ Animation Event khi clip Attack kết thúc
    /// </summary>
    public void OnAttackFinishedAnimationEvent()
    {
        isAttacking = false;
    }

    /// <summary>
    /// Thực hiện quét va chạm hình cầu và áp dụng sát thương lên mục tiêu
    /// </summary>
    private void ExecuteHitCheck()
    {
        ResolveCamera();
        Transform originTransform = playerCamera != null ? playerCamera : transform;
        Vector3 origin = originTransform.position;
        Vector3 direction = originTransform.forward;

        // Tính toán tổng sát thương = Sát thương vũ khí + Bonus từ chỉ số PlayerStats (nếu có)
        int totalDamage = attackDamage;
        if (playerStats == null) ResolvePlayerStats();
        if (playerStats != null)
        {
            int bonus = Mathf.RoundToInt(playerStats.currentDamage - playerStats.baseDamage);
            if (bonus > 0)
            {
                totalDamage += bonus;
            }
        }

        HashSet<GameObject> hitEntities = new HashSet<GameObject>();
        bool hasHitAnything = false;

        // 1. Quét SphereCast dọc theo hướng nhìn của camera
        RaycastHit[] hits = Physics.SphereCastAll(origin, attackRadius, direction, attackRange, hitMask, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null) continue;

            // Bỏ qua chính Player
            if (player != null && (hit.collider.transform.IsChildOf(player.transform) || hit.collider.gameObject == player.gameObject))
            {
                continue;
            }

            GameObject rootObj = hit.collider.transform.root.gameObject;
            if (hitEntities.Contains(rootObj)) continue;
            hitEntities.Add(rootObj);

            bool hitValid = false;

            // Kiểm tra ZombieAI
            ZombieAI zombie = hit.collider.GetComponentInParent<ZombieAI>();
            if (zombie != null)
            {
                zombie.TakeDamage(totalDamage);
                hitValid = true;
            }

            // Kiểm tra Target
            Target target = hit.collider.GetComponentInParent<Target>();
            if (target != null)
            {
                target.OnHit();
                hitValid = true;
            }

            if (hitValid)
            {
                hasHitAnything = true;
                SpawnHitEffect(hit.point, hit.normal);
            }
        }

        // 2. Dự phòng OverlapSphere sát trước mặt người chơi nếu SphereCast trượt do mục tiêu áp quá gần
        if (!hasHitAnything)
        {
            Vector3 closeCenter = origin + direction * (attackRange * 0.5f);
            Collider[] overlapHits = Physics.OverlapSphere(closeCenter, attackRadius, hitMask, QueryTriggerInteraction.Ignore);

            foreach (Collider col in overlapHits)
            {
                if (col == null) continue;
                if (player != null && (col.transform.IsChildOf(player.transform) || col.gameObject == player.gameObject)) continue;

                GameObject rootObj = col.transform.root.gameObject;
                if (hitEntities.Contains(rootObj)) continue;
                hitEntities.Add(rootObj);

                ZombieAI zombie = col.GetComponentInParent<ZombieAI>();
                if (zombie != null)
                {
                    zombie.TakeDamage(totalDamage);
                    hasHitAnything = true;
                    SpawnHitEffect(col.bounds.center, -direction);
                    break;
                }

                Target target = col.GetComponentInParent<Target>();
                if (target != null)
                {
                    target.OnHit();
                    hasHitAnything = true;
                    SpawnHitEffect(col.bounds.center, -direction);
                    break;
                }
            }
        }

        // Phát âm thanh khi chém trúng bất kỳ mục tiêu nào
        if (hasHitAnything)
        {
            PlaySound(hitSound);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEffectSound(clip);
        }
        else if (audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void SpawnHitEffect(Vector3 position, Vector3 normal)
    {
        if (hitEffectPrefab != null)
        {
            Quaternion rot = normal != Vector3.zero ? Quaternion.LookRotation(normal) : Quaternion.identity;
            ParticleSystem ps = Instantiate(hitEffectPrefab, position, rot);
            Destroy(ps.gameObject, 2f);
        }
    }

    /// <summary>
    /// Vũ khí cận chiến không dùng cơ chế nạp đạn
    /// </summary>
    public override void Reload()
    {
        // No-op cho vũ khí cận chiến
    }

    private void OnDrawGizmosSelected()
    {
        ResolveCamera();
        Transform t = playerCamera != null ? playerCamera : transform;
        if (t == null) return;

        Gizmos.color = Color.yellow;
        Vector3 endPoint = t.position + t.forward * attackRange;
        Gizmos.DrawLine(t.position, endPoint);
        Gizmos.DrawWireSphere(endPoint, attackRadius);
    }
}
