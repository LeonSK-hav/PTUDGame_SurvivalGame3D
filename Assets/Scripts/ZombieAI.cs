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

    [Header("UI Health Bar (Screen Space)")]
    public GameObject healthUIPanel;        // Kéo GameObject Panel thanh máu trên UI chung vào đây
    public Slider healthSlider;             // Slider thanh máu
    public TextMeshProUGUI healthText;      // Text hiển thị số máu (vd: 100/100)
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
    bool isHealthBarShown = false;
    private Camera mainCamera;

    private void Start()
    {
        currentHealth = maxHealth;
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        agent.SetDestination(target.position);
        agent.speed = Runner ? runSpeed : walkSpeed;

        mainCamera = Camera.main;

        // Ban đầu ẩn thanh máu đi
        if (healthUIPanel != null)
        {
            healthUIPanel.SetActive(false);
        }

        UpdateHealthUI();
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

    // Dùng LateUpdate để cập nhật vị trí thanh máu trên màn hình theo vị trí đầu của Zombie
    void LateUpdate()
    {
        if (healthUIPanel != null && healthUIPanel.activeSelf && mainCamera != null)
        {
            // Lấy vị trí đỉnh đầu của zombie trong không gian 3D rồi chuyển sang tọa độ màn hình (Screen Point)
            Vector3 worldPosition = transform.position + headOffset;
            Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

            // Kiểm tra xem zombie có đang ở trước mặt camera không (tránh hiển thị khi ở sau lưng)
            if (screenPosition.z > 0)
            {
                healthUIPanel.transform.position = screenPosition;
            }
            else
            {
                // Nếu zombie ở sau lưng camera thì ẩn thanh máu đi
                healthUIPanel.SetActive(false);
            }
        }
    }

    void Die()
    {
        dead = true;
        currentHealth = 0;
        agent.ResetPath();
        agent.speed = 0;
        anim.SetBool("Death", true);

        // Khi chết thì ẩn thanh máu đi
        if (healthUIPanel != null)
        {
            healthUIPanel.SetActive(false);
        }

        Destroy(gameObject, 5f);
    }
    
    public void TakeDamage(int Damage)
    {
        if (dead) return;

        currentHealth -= Damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // HIỂN THỊ THANH MÁU LẦN ĐẦU KHI NHẬN SÁT THƯƠNG
        if (!isHealthBarShown && healthUIPanel != null)
        {
            healthUIPanel.SetActive(true);
            isHealthBarShown = true;
        }

        UpdateHealthUI();

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