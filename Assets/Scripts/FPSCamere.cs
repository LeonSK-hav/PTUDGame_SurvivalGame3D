using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSCamere : MonoBehaviour
{
    public Transform playerBody;
    public float yClamp;
    public float Senstivity;

    float Xrotation;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 mouse = InputManager.Instance.playerInput.Player.Look.ReadValue<Vector2>();

        float mouseX = mouse.x * Senstivity * Time.deltaTime;
        float mouseY = mouse.y * Senstivity * Time.deltaTime;

        Xrotation -= mouseY;
        Xrotation = Mathf.Clamp(Xrotation, -yClamp, yClamp);

        transform.localRotation = Quaternion.Euler(Xrotation, 0, 0);
        playerBody.Rotate(Vector3.up * mouseX);
    }
}
