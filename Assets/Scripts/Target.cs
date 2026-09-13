using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{
    MeshRenderer mesh;
    Color originalColor;

    // Start is called once before the first execution of Update after the Mon...
    void Start()
    {
        mesh = GetComponent<MeshRenderer>();
        originalColor = mesh.material.color;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnHit()
    {
        mesh.material.color = Color.red;
        Invoke("ResetMat", 0.2f);
    }

    void ResetMat()
    {
        mesh.material.color = originalColor;
    }
}
