using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

public class ZombieAI : MonoBehaviour
{
    public Transform target;
    public bool Runner = false;

    public float walkSpeed = 2f;
    public float runSpeed = 3.5f;

    [Header("Health")]
    public int currentHealth;
    public int maxHealth = 100;

    [Header("UI Health Bar Prefab")]
    public GameObject healthUIPrefab;       // Kéo Prefab "ZombieHeathUI" vào đây
    private GameObject instantiatedHealthUI;// Bản sao thanh máu riêng của con zombie này
    private Slider healthSlider;            
    private TextMeshProUGUI healthText;     
    public Vector3 headOffset = new Vector3(0, 2.2f, 0); // Khoảng cách từ gốc zombie lên đầu

    [Header("Combat")]
    public float attackRange = 2f;
    public float attackCooldown = 1f;
    public float hitDuration = 0.4f;

    float attackTime = 0f;

    private NavMeshAgent agent;
    Animator anim;

    bool dead;
    bool hit;
    private Camera mainCamera;
    private Canvas mainCanvas;

    private void Start()
    {
        currentHealth = maxHealth;
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        agent.SetDestination(target.position);
        agent.speed = Runner ? runSpeed : walkSpeed;

        mainCamera = Camera.main;

        // Tự động tìm Canvas chính trong Scene
        Canvas[] canvases = FindObjectsOfType<Canvas>();
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
            // THÊM chữ 'false' vào cuối để giữ đúng hệ tọa độ cục bộ của UI Canvas
            instantiatedHealthUI = Instantiate(healthUIPrefab, mainCanvas.transform, false);
            
            healthSlider = instantiatedHealthUI.GetComponentInChildren<Slider>();
            healthText = instantiatedHealthUI.GetComponentInChildren<TextMeshProUGUI>();

            instantiatedHealthUI.SetActive(false);
            UpdateHealthUI();
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
                    // Ép vị trí của thanh máu đi theo tọa độ local của Canvas chính một cách chính xác tuyệt đối
                    Vector2 localPoint;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRect,
                        screenPosition,
                        null, // Truyền null vì Canvas là Screen Space - Overlay
                        out localPoint
                    );
                    rectTransform.anchoredPosition = localPoint;
                }

                // Luôn giữ Scale bằng 1 để thanh máu không bị phóng to hay thu nhỏ bất thường
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

        if (distance > attackRange)
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
        
        anim.SetFloat("Speed", agent.velocity.magnitude);
        anim.SetBool("Run", Runner);

        if (currentHealth <= 0 && !dead)
        {
            Die();
        }
    }

    

    void Die()
    {
        dead = true;
        currentHealth = 0;
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

        currentHealth -= Damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

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
        if (Time.time >= attackTime)
        {
            anim.SetTrigger("Attack");
            attackTime = Time.time + attackCooldown;
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
        agent.isStopped = false;
        hit = false;
    }
}