using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombieAI : MonoBehaviour
{
    public Transform target;
    public bool Runner = false;

    [Header("Health")]
    public int currentHealth;
    public int maxHealth = 100;

    private NavMeshAgent agent;
    Animator anim;

    bool dead;
    private void Start()
    {
        currentHealth = maxHealth;
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        agent.SetDestination(target.position);

    }
    // Update is called once per frame
    void Update()
    {
        if (dead)return;
        anim.SetFloat("Speed",agent.velocity.magnitude);
        anim.SetBool("Run",Runner);
        if(currentHealth <= 0)
        {
            dead = true;
            currentHealth = 0;
            agent.ResetPath();
            agent.speed = 0;

            //GetComponent<MeshRenderer>().material.color = Color.red;
            anim.SetBool("Death", true);
            Destroy(gameObject, 5f);
        }
    }
    
    public void TakeDamage(int Damage)
    {
        currentHealth -= Damage;

    }
}
