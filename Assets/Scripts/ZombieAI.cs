using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieAI : MonoBehaviour
{
    public Transform player;

    public float DetectionRange = 10f;
    public float moveSpeed = 2f;
    public float stopDistance = 2f;
    public float rotSpeed = 10f;


    [Header("Health")]
    public int currentHealth;
    public int maxHealth = 100;

    bool dead;
    private void Start()
    {
        currentHealth = maxHealth;
    }
    // Update is called once per frame
    void Update()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= DetectionRange && !dead)
        {
            FollowPlayer(distance);
        }

        if (dead)
        {
            GetComponent<MeshRenderer>().material.color = Color.red;
        }
    }
    void FollowPlayer(float distance)
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0f;

        Quaternion targetRot = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotSpeed * Time.deltaTime);

        if (distance > stopDistance)
        {
            transform.position += direction * moveSpeed * Time.deltaTime;
        }
    }

    public void TakeDamage(int Damage)
    {
        currentHealth -= Damage;

        if (currentHealth < 0)
        {
            dead = true;
            currentHealth = 0;
            GetComponent<MeshRenderer>().material.color = Color.red;
            Destroy(gameObject, 5f);
        }
    }
}
