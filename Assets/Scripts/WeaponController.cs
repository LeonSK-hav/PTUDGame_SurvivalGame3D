using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class WeaponController : MonoBehaviour
{
    public float BulletRange = 100f;
    public float FireRate = 0.5f;

    public int currentBullets = 0;
    public int BulletsInMag = 8;

    public int BulletsDamage = 25;
    public ParticleSystem muzzleFlash;
    public AudioSource source;
    public AudioClip shootSound;
    public Transform playerCamera;

    Animator animator;
    PlayerController player;
    float nextFireTime = 5f;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        player = transform.parent.root.GetComponent<PlayerController>();
        currentBullets = BulletsInMag;
    }

    // Update is called once per frame
    void Update()
    {
        if(InputManager.Instance.playerInput.Player.Fire.WasPressedThisFrame() || InputManager.Instance.playerInput.Player.Fire.IsPressed())
        {
            if (!player.IsRunning)
            {
                if (Time.time >= nextFireTime &&  currentBullets > 0)
                {
                    Shoot();
                    nextFireTime = Time.time + FireRate;

                }
            }
        }

        if(Keyboard.current.rKey.wasPressedThisFrame)
        {
            currentBullets = BulletsInMag;
        }
        animator.SetBool("Walk", player.IsWalking);
        animator.SetBool("Run", player.IsRunning);
    }

    void Shoot()
    {
        currentBullets--;
        if(muzzleFlash)
        {
            muzzleFlash.Play();
        }

        animator.SetTrigger("Shoot");

        if (source && shootSound)
        {
            source.PlayOneShot(shootSound);
        }

        RaycastHit hit;
        if(Physics.Raycast(playerCamera.position, playerCamera.forward, out hit, BulletRange))
        {
            Target t = hit.collider.GetComponent<Target>();
            ZombieAI z = hit.collider.GetComponent<ZombieAI>();
            Debug.Log(hit.collider.name);

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
