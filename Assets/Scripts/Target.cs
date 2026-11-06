using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{
    [Header("Hit Flash Settings")]
    public Color hitColor = Color.red;
    public float flashDuration = 0.2f;

    private struct MatColorInfo
    {
        public Material mat;
        public string prop;
        public Color originalColor;
    }

    private List<MatColorInfo> colorList = new List<MatColorInfo>();

    void Start()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            if (r is ParticleSystemRenderer) continue;
            foreach (var mat in r.materials)
            {
                if (mat == null) continue;
                string prop = mat.HasProperty("_BaseColor") ? "_BaseColor" :
                              mat.HasProperty("_Color") ? "_Color" : null;
                if (prop != null)
                {
                    colorList.Add(new MatColorInfo
                    {
                        mat = mat,
                        prop = prop,
                        originalColor = mat.GetColor(prop)
                    });
                }
            }
        }
    }

    public void OnHit()
    {
        SetColor(hitColor);
        CancelInvoke(nameof(ResetMat));
        Invoke(nameof(ResetMat), flashDuration);
    }

    void ResetMat()
    {
        for (int i = 0; i < colorList.Count; i++)
        {
            var item = colorList[i];
            if (item.mat != null)
            {
                item.mat.SetColor(item.prop, item.originalColor);
            }
        }
    }

    void SetColor(Color c)
    {
        for (int i = 0; i < colorList.Count; i++)
        {
            var item = colorList[i];
            if (item.mat != null)
            {
                item.mat.SetColor(item.prop, c);
            }
        }
    }
}
