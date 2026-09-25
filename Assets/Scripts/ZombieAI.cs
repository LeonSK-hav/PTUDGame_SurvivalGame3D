using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombieAI : MonoBehaviour
{
    public Transform target;
    public bool Runner = false;

    public float walkSpeed = 2f;
    public float runSpeed = 3.5f;

    [Header("Health")]
    public int currentHealth;
    public int maxHealth = 100;

    [Header("Combat")]
    public float attackRange = 2f;
    public float attackCooldown = 1f;
    public float hitDuration = 0.4f;

    float attackTime = 0f;

    private NavMeshAgent agent;
    Animator anim;

    bool dead;
    bool hit;
    private void Start()
    {
        currentHealth = maxHealth;
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        agent.SetDestination(target.position);

        agent.speed = Runner ? runSpeed : walkSpeed;

    }
    // Update is called once per frame
    void Update()
    {
        if (dead)return;
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
        
        anim.SetFloat("Speed",agent.velocity.magnitude);
        anim.SetBool("Run",Runner);
        if(currentHealth <= 0 && !dead)
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
        //GetComponent<MeshRenderer>().material.color = Color.red;
        anim.SetBool("Death", true);
        Destroy(gameObject, 5f);
    }
    
    public void TakeDamage(int Damage)
    {
        currentHealth -= Damage;
        anim.SetTrigger("hit");
        StartCoroutine(HitDuration());

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
