using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

public class ZombieAI : MonoBehaviour
{
    public Transform target;
    public PlayerHealth playerHealth;
    public bool Runner = false;

    public float walkSpeed = 2f;
    public float runSpeed = 3.5f;

    [Header("Health")]
    public int currentHealth;
    public int maxHealth = 100;
    public int expReward = 20;

    [Header("UI Health Bar Prefab")]
    public GameObject healthUIPrefab;       // Kéo Prefab "Zombie_HP_Bar" vào đây
    private GameObject instantiatedHealthUI;// Bản sao thanh máu riêng của con zombie này
    private Slider healthSlider;            
    private TextMeshProUGUI healthText;     
    public Vector3 headOffset = new Vector3(0, 2.2f, 0); // Khoảng cách từ gốc zombie lên đầu
    private bool isHealthBarVisible = false;

    [Header("Combat")]
    public int damageAmount = 10;
    public float attackDistance = 2f;
    public float attackCooldown = 1.2f;
    public float hitDuration = 0.4f;

    [Header("Detection & Movement")]
    public float detectionRange = 15f;    // Vùng phát hiện player giới hạn trong khoảng nhỏ
    public float loseTargetRange = 25f;   // Khoảng cách mất dấu player
    private bool isAggroed = false;

    [Header("Wander Settings (Di chuyển ngẫu nhiên)")]
    public bool enableWander = true;      // Cho phép zombie di chuyển ngẫu nhiên khi chưa phát hiện player
    public float wanderRadius = 10f;      // Bán kính di chuyển ngẫu nhiên xung quanh khu vực
    public float minWanderWaitTime = 2f;  // Thời gian đứng nghỉ tối thiểu giữa các lần đi
    public float maxWanderWaitTime = 5f;  // Thời gian đứng nghỉ tối đa
    public float wanderSpeed = 1.8f;      // Tốc độ di chuyển khi đi dạo/tuần tra
    
    private Vector3 initialPosition;      // Vị trí xuất phát của zombie
    private float wanderTimer = 0f;
    private float wanderMoveTimeout = 0f;
    private bool isWaitingAtPoint = false;

    [Header("Hit Flash Effect (Red on Damage)")]
    public Color hitColor = Color.red;    // Hiệu ứng màu đỏ biểu thị zombie bị trừ máu (theo commit cũ)
    public float flashDuration = 0.2f;

    float nextAttackTime;

    private NavMeshAgent agent;
    private CapsuleCollider col;
    Animator anim;

    bool dead;
    bool hit;
    private Camera mainCamera;
    private Canvas mainCanvas;

    private struct MaterialColorData
    {
        public Material material;
        public Color originalColor;
        public string propName;
    }

    private List<MaterialColorData> materialColorDataList = new List<MaterialColorData>();
    private Coroutine flashCoroutine;

    private void Start()
    {
        currentHealth = maxHealth;
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        col = GetComponent<CapsuleCollider>();

        initialPosition = transform.position;
        wanderTimer = Random.Range(0.5f, minWanderWaitTime);

        if (target == null)
        {
            Debug.LogError($"{name} requires a target Transform.", this);
            enabled = false;
            return;
        }

        if (playerHealth == null)
        {
            playerHealth = target.GetComponentInParent<PlayerHealth>();
        }

        if (playerHealth == null)
        {
            Debug.LogError($"{name} requires a PlayerHealth reference.", this);
            enabled = false;
            return;
        }

        mainCamera = Camera.main;

        // Lưu trữ Renderers và Material để đổi màu đỏ khi nhận sát thương
        InitializeHitFlashMaterials();

        // Tự động tìm Canvas chính trong Scene (ưu tiên root Canvas ScreenSpaceOverlay đang active)
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var canvas in canvases)
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay && canvas.isRootCanvas && canvas.isActiveAndEnabled)
            {
                mainCanvas = canvas;
                break;
            }
        }
        if (mainCanvas == null && canvases.Length > 0)
        {
            foreach (var c in canvases)
            {
                if (c.isRootCanvas && c.isActiveAndEnabled) { mainCanvas = c; break; }
            }
        }
        if (mainCanvas == null && canvases.Length > 0) mainCanvas = canvases[0];

        // Tự động sinh ra bản sao thanh máu riêng cho zombie này
        if (healthUIPrefab != null && mainCanvas != null)
        {
            instantiatedHealthUI = Instantiate(healthUIPrefab, mainCanvas.transform, false);

            // Hủy bỏ CanvasScaler và GraphicRaycaster để không xung đột scale và không cản tia bắn đạn
            var childRaycaster = instantiatedHealthUI.GetComponent<GraphicRaycaster>();
            if (childRaycaster != null) Destroy(childRaycaster);

            var childScaler = instantiatedHealthUI.GetComponent<CanvasScaler>();
            if (childScaler != null) Destroy(childScaler);

            // Giữ Canvas làm Sub-Canvas và ưu tiên thứ tự vẽ lên trên
            var childCanvas = instantiatedHealthUI.GetComponent<Canvas>();
            if (childCanvas != null)
            {
                childCanvas.overrideSorting = true;
                childCanvas.sortingOrder = 50;
            }

            // Chuẩn hóa Anchor và Pivot về chính giữa (0.5, 0.5) để ghim chuẩn tâm thanh máu
            RectTransform rt = instantiatedHealthUI.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.localScale = Vector3.one;
            }

            healthSlider = instantiatedHealthUI.GetComponentInChildren<Slider>();
            healthText = instantiatedHealthUI.GetComponentInChildren<TextMeshProUGUI>();

            // Mặc định ban đầu ẩn thanh máu
            instantiatedHealthUI.SetActive(false);
            isHealthBarVisible = false;
            UpdateHealthUI();
        }

        // Kiểm tra tầm phát hiện ban đầu
        float initialDistance = Vector3.Distance(transform.position, target.position);
        if (initialDistance <= detectionRange)
        {
            isAggroed = true;
            agent.speed = Runner ? runSpeed : walkSpeed;
            agent.SetDestination(target.position);
        }
        else if (enableWander)
        {
            // Bắt đầu di chuyển ngẫu nhiên
            StartNewWander();
        }
        else
        {
            agent.isStopped = true;
        }
    }

    // Khởi tạo danh sách material để tạo hiệu ứng nhấp nháy đỏ khi bị bắn
    void InitializeHitFlashMaterials()
    {
        materialColorDataList.Clear();
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            if (rend is ParticleSystemRenderer) continue;

            foreach (var mat in rend.materials)
            {
                if (mat == null) continue;

                string prop = null;
                if (mat.HasProperty("_BaseColor")) prop = "_BaseColor";
                else if (mat.HasProperty("_Color")) prop = "_Color";

                if (prop != null)
                {
                    materialColorDataList.Add(new MaterialColorData
                    {
                        material = mat,
                        originalColor = mat.GetColor(prop),
                        propName = prop
                    });
                }
            }
        }
    }

    // Dùng LateUpdate để ghim chuẩn xác thanh máu lên đầu Zombie
    void LateUpdate()
    {
        if (instantiatedHealthUI == null) return;

        // Chỉ hiển thị và cập nhật vị trí thanh máu nếu zombie đã từng bị bắn trúng (isHealthBarVisible == true)
        if (!isHealthBarVisible || dead)
        {
            if (instantiatedHealthUI.activeSelf) instantiatedHealthUI.SetActive(false);
            return;
        }

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null || mainCanvas == null) return;

        // 1. Xác định vị trí đỉnh đầu Zombie trong không gian 3D (dựa theo CapsuleCollider để không bị lệch khi xoay)
        Vector3 headWorldPos;
        if (col != null)
        {
            headWorldPos = new Vector3(col.bounds.center.x, col.bounds.max.y + 0.25f, col.bounds.center.z);
        }
        else
        {
            float height = (headOffset.y > 0.5f) ? headOffset.y : 2.2f;
            headWorldPos = transform.position + Vector3.up * height;
        }

        // 2. Kiểm tra xem Zombie có đang ở phía trước tầm nhìn của Camera không
        Vector3 toZombie = headWorldPos - mainCamera.transform.position;
        bool isBehindCamera = Vector3.Dot(mainCamera.transform.forward, toZombie) <= 0.1f;

        if (isBehindCamera)
        {
            if (instantiatedHealthUI.activeSelf) instantiatedHealthUI.SetActive(false);
            return;
        }

        // 3. Chuyển đổi vị trí 3D sang tọa độ màn hình (Screen Point)
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(headWorldPos);

        if (screenPosition.z > 0)
        {
            RectTransform canvasRect = mainCanvas.GetComponent<RectTransform>();
            RectTransform uiRect = instantiatedHealthUI.GetComponent<RectTransform>();

            if (canvasRect != null && uiRect != null)
            {
                Vector2 localPoint;
                Camera camForRect = (mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : mainCamera;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPosition,
                    camForRect,
                    out localPoint))
                {
                    uiRect.anchoredPosition = localPoint;
                    uiRect.localScale = Vector3.one;

                    if (!instantiatedHealthUI.activeSelf)
                    {
                        instantiatedHealthUI.SetActive(true);
                    }
                }
            }
        }
        else
        {
            if (instantiatedHealthUI.activeSelf) instantiatedHealthUI.SetActive(false);
        }
    }

    void Update()
    {
        if (dead) return;
        float distance = Vector3.Distance(transform.position, target.position);

        // Vùng phát hiện player giới hạn trong khoảng nhỏ (detectionRange) hoặc đã bị kích động (isAggroed)
        if (distance <= detectionRange || isAggroed)
        {
            isAggroed = true;

            // Nếu player chạy thoát vượt quá loseTargetRange, zombie mất dấu và quay lại đi dạo ngẫu nhiên
            if (distance > loseTargetRange)
            {
                isAggroed = false;
                agent.ResetPath();
                isWaitingAtPoint = true;
                wanderTimer = Random.Range(minWanderWaitTime, maxWanderWaitTime);
            }
            else if (distance > attackDistance)
            {
                agent.isStopped = false;
                agent.SetDestination(target.position);
                agent.speed = Runner ? runSpeed : walkSpeed;
                anim.SetBool("Run", Runner);
            }
            else
            {
                agent.isStopped = true;
                TryAttack();
            }
        }
        else
        {
            // PLAYER Ở NGOÀI VÙNG PHÁT HIỆN: ZOMBIE DI CHUYỂN NGẪU NHIÊN (WANDER)
            anim.SetBool("Run", false);

            if (enableWander)
            {
                UpdateWanderState();
            }
            else
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
        }
        
        anim.SetFloat("Speed", agent.velocity.magnitude);

        if (currentHealth <= 0 && !dead)
        {
            Die();
        }
    }

    // Logic di chuyển ngẫu nhiên (Wander / Roam) khi player chưa vào tầm
    void UpdateWanderState()
    {
        if (isWaitingAtPoint)
        {
            agent.isStopped = true;
            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0f)
            {
                StartNewWander();
            }
        }
        else
        {
            wanderMoveTimeout -= Time.deltaTime;

            // Kiểm tra xem đã đến điểm đích chưa hoặc bị kẹt quá thời gian
            if ((!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f) || wanderMoveTimeout <= 0f)
            {
                isWaitingAtPoint = true;
                agent.isStopped = true;
                wanderTimer = Random.Range(minWanderWaitTime, maxWanderWaitTime);
            }
            else
            {
                agent.isStopped = false;
                agent.speed = wanderSpeed;
            }
        }
    }

    void StartNewWander()
    {
        Vector3 newPoint;
        if (GetRandomWanderPoint(out newPoint))
        {
            isWaitingAtPoint = false;
            wanderMoveTimeout = 10f; // Tối đa 10s cho một hành trình đi dạo
            agent.isStopped = false;
            agent.speed = wanderSpeed;
            agent.SetDestination(newPoint);
        }
        else
        {
            isWaitingAtPoint = true;
            wanderTimer = Random.Range(minWanderWaitTime, maxWanderWaitTime);
        }
    }

    bool GetRandomWanderPoint(out Vector3 result)
    {
        // Ưu tiên tìm điểm ngẫu nhiên xung quanh khu vực ban đầu của zombie
        Vector3 center = (initialPosition != Vector3.zero) ? initialPosition : transform.position;

        for (int i = 0; i < 5; i++)
        {
            Vector2 randCircle = Random.insideUnitCircle * wanderRadius;
            Vector3 randomPos = center + new Vector3(randCircle.x, 0, randCircle.y);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPos, out hit, wanderRadius, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }

        // Dự phòng: Tìm điểm ngẫu nhiên xung quanh vị trí hiện tại
        for (int i = 0; i < 5; i++)
        {
            Vector2 randCircle = Random.insideUnitCircle * wanderRadius;
            Vector3 randomPos = transform.position + new Vector3(randCircle.x, 0, randCircle.y);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPos, out hit, 5f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }

        result = transform.position;
        return false;
    }

    void Die()
    {
        if (dead) return;

        dead = true;
        currentHealth = 0;
        isHealthBarVisible = false;

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        // Bổ sung code từ commit cũ: biểu thị màu đỏ khi chết
        ApplyColorToMaterials(hitColor);

        if (PlayerLevelSystem.Instance != null)
        {
            PlayerLevelSystem.Instance.AddExp(expReward);
        }
        else
        {
            Debug.LogError("Không tìm thấy PlayerLevelSystem để cộng EXP khi Zombie chết.", this);
        }

        agent.ResetPath();
        agent.speed = 0;
        anim.SetBool("Death", true);

        // Khi chết (máu về 0), xóa luôn thanh máu riêng của zombie này
        if (instantiatedHealthUI != null)
        {
            Destroy(instantiatedHealthUI);
        }

        Destroy(gameObject, 5f);
    }
    
    public void TakeDamage(int Damage)
    {
        if (dead) return;

        // Khi bị bắn trúng, zombie sẽ lập tức phát hiện và truy đuổi player
        isAggroed = true;

        currentHealth -= Damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // HIỆU ỨNG MÀU ĐỎ BIỂU THỊ ZOMBIE BỊ TRỪ MÁU (Theo yêu cầu khôi phục từ commit cũ)
        TriggerHitFlash();

        // MỖI KHI BỊ GIẢM MÁU THÌ KÍCH HOẠT HIỂN THỊ THANH MÁU TRÊN ĐẦU
        isHealthBarVisible = true;
        if (instantiatedHealthUI != null)
        {
            instantiatedHealthUI.SetActive(true);
        }

        UpdateHealthUI();

        // Nếu máu về 0 thì gọi Die ngay
        if (currentHealth <= 0 && !dead)
        {
            Die();
            return;
        }

        anim.SetTrigger("hit");
        StartCoroutine(HitDuration());
    }

    void TriggerHitFlash()
    {
        if (dead) return;

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            ResetMaterialColors();
        }
        flashCoroutine = StartCoroutine(FlashRedRoutine());
    }

    IEnumerator FlashRedRoutine()
    {
        ApplyColorToMaterials(hitColor);

        yield return new WaitForSeconds(flashDuration);

        if (!dead)
        {
            ResetMaterialColors();
        }
        flashCoroutine = null;
    }

    void ApplyColorToMaterials(Color color)
    {
        for (int i = 0; i < materialColorDataList.Count; i++)
        {
            var data = materialColorDataList[i];
            if (data.material != null)
            {
                data.material.SetColor(data.propName, color);
            }
        }
    }

    void ResetMaterialColors()
    {
        for (int i = 0; i < materialColorDataList.Count; i++)
        {
            var data = materialColorDataList[i];
            if (data.material != null)
            {
                data.material.SetColor(data.propName, data.originalColor);
            }
        }
    }

    void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (healthText != null)
        {
            healthText.text = $"{currentHealth}/{maxHealth}";
        }
    }

    void TryAttack()
    {
        if (hit || dead) return;
        if (Time.time >= nextAttackTime)
        {
            anim.SetTrigger("Attack");
            playerHealth.TakeDamage(damageAmount);
            nextAttackTime = Time.time + Mathf.Max(0f, attackCooldown);
        }
    }

    public void Damage()
    {
        Debug.Log("Damaged");
    }

    IEnumerator HitDuration()
    {
        hit = true;
        agent.isStopped = true;
        yield return new WaitForSeconds(hitDuration);
        if (!dead && isAggroed)
        {
            agent.isStopped = false;
        }
        hit = false;
    }
}
