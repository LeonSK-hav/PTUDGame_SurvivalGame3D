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
    public GameObject healthUIPrefab;       // Kéo Prefab "ZombieHeathUI" vào đây
    private GameObject instantiatedHealthUI;// Bản sao thanh máu riêng của con zombie này
    private Slider healthSlider;            
    private TextMeshProUGUI healthText;     
    public Vector3 headOffset = new Vector3(0, 2.2f, 0); // Khoảng cách từ gốc zombie lên đầu

    [Header("Combat")]
    public int damageAmount = 10;
    public float attackDistance = 2f;
    public float attackCooldown = 1.2f;
    public float hitDuration = 0.4f;

    [Header("Detection & Movement")]
    public float detectionRange = 15f;    // Vùng phát hiện player giới hạn trong khoảng nhỏ
    public float loseTargetRange = 25f;   // Khoảng cách mất dấu player
    private bool isAggroed = false;

    [Header("Hit Flash Effect (Red on Damage)")]
    public Color hitColor = Color.red;    // Hiệu ứng màu đỏ biểu thị zombie bị trừ máu (theo commit cũ)
    public float flashDuration = 0.2f;

    float nextAttackTime;

    private NavMeshAgent agent;
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

        agent.speed = Runner ? runSpeed : walkSpeed;

        // Giới hạn tầm phát hiện ngay từ ban đầu: chỉ di chuyển nếu player nằm trong detectionRange
        float initialDistance = Vector3.Distance(transform.position, target.position);
        if (initialDistance <= detectionRange)
        {
            isAggroed = true;
            agent.SetDestination(target.position);
        }
        else
        {
            agent.isStopped = true;
        }

        mainCamera = Camera.main;

        // Lưu trữ Renderers và Material để đổi màu đỏ khi nhận sát thương
        InitializeHitFlashMaterials();

        // Tự động tìm Canvas chính trong Scene
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var canvas in canvases)
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                mainCanvas = canvas;
                break;
            }
        }
        if (mainCanvas == null && canvases.Length > 0) mainCanvas = canvases[0];

        // Tự động sinh ra bản sao thanh máu
        if (healthUIPrefab != null && mainCanvas != null)
        {
            instantiatedHealthUI = Instantiate(healthUIPrefab, mainCanvas.transform, false);
            
            healthSlider = instantiatedHealthUI.GetComponentInChildren<Slider>();
            healthText = instantiatedHealthUI.GetComponentInChildren<TextMeshProUGUI>();

            instantiatedHealthUI.SetActive(false);
            UpdateHealthUI();
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
        if (instantiatedHealthUI != null && instantiatedHealthUI.activeSelf && mainCamera != null && mainCanvas != null)
        {
            // Lấy vị trí đỉnh đầu của Zombie trong không gian 3D
            Vector3 worldPosition = transform.position + headOffset;
            
            // Chuyển đổi tọa độ 3D thành tọa độ màn hình (Screen Point)
            Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

            // Kiểm tra xem Zombie có đang ở trước mặt camera không
            if (screenPosition.z > 0)
            {
                RectTransform rectTransform = instantiatedHealthUI.GetComponent<RectTransform>();
                RectTransform canvasRect = mainCanvas.GetComponent<RectTransform>();

                if (rectTransform != null && canvasRect != null)
                {
                    Vector2 localPoint;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRect,
                        screenPosition,
                        null, // Truyền null vì Canvas là Screen Space - Overlay
                        out localPoint
                    );
                    rectTransform.anchoredPosition = localPoint;
                }

                instantiatedHealthUI.transform.localScale = Vector3.one;
            }
            else
            {
                instantiatedHealthUI.SetActive(false);
            }
        }
    }

    void Update()
    {
        if (dead) return;
        float distance = Vector3.Distance(transform.position, target.position);

        // Vùng phát hiện player giới hạn trong khoảng nhỏ (detectionRange)
        if (distance <= detectionRange || isAggroed)
        {
            isAggroed = true;

            // Nếu player chạy thoát vượt quá loseTargetRange, zombie mất dấu và dừng lại
            if (distance > loseTargetRange)
            {
                isAggroed = false;
                agent.isStopped = true;
                agent.ResetPath();
            }
            else if (distance > attackDistance)
            {
                agent.isStopped = false;
                agent.SetDestination(target.position);
                agent.speed = Runner ? runSpeed : walkSpeed;
            }
            else
            {
                agent.isStopped = true;
                TryAttack();
            }
        }
        else
        {
            // Player ngoài tầm phát hiện: zombie đứng yên
            agent.isStopped = true;
            agent.ResetPath();
        }
        
        anim.SetFloat("Speed", agent.velocity.magnitude);
        anim.SetBool("Run", Runner);

        if (currentHealth <= 0 && !dead)
        {
            Die();
        }
    }

    void Die()
    {
        if (dead) return;

        dead = true;
        currentHealth = 0;

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        // Bổ sung code từ commit cũ: biểu thị màu đỏ khi chết
        // (Commit 648de30: GetComponent<MeshRenderer>().material.color = Color.red;)
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

        // MỖI KHI BỊ GIẢM MÁU THÌ HIỆN THANH MÁU LÊN
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
