#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public class ShoeIconAndPhysicsGenerator : MonoBehaviour
{
    private const string PrefabFolderPath = "Assets/npc_casual_set_00/PrefabShoe";
    private const string OutputIconFolder = "Assets/GeneratedIcons";
    private const string WorldItemPrefabFolder = "Assets/Prefab";

    // Đổi thành 180f, 90f, hoặc -90f nếu mũi giày quay sai hướng so với camera
    private const float YawOffset = 180f;
    private const float Padding = 1.35f;

    [MenuItem("Tools/Tạo Icon Giày & Tự Động Thêm Vật Lý")]
    public static void GenerateShoeIconsAndPhysics()
    {
        if (!AssetDatabase.IsValidFolder(OutputIconFolder))
        {
            AssetDatabase.CreateFolder("Assets", "GeneratedIcons");
        }

        if (!AssetDatabase.IsValidFolder(WorldItemPrefabFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Prefab");
        }

        GameObject camObj = new GameObject("ShoeIconCaptureCamera");
        Camera capCam = camObj.AddComponent<Camera>();
        capCam.clearFlags = CameraClearFlags.SolidColor;
        capCam.backgroundColor = new Color(0, 0, 0, 0);
        capCam.orthographic = true;
        capCam.nearClipPlane = 0.01f;
        capCam.farClipPlane = 100f;

        Light tempLightObj = new GameObject("ShoeIconLight").AddComponent<Light>();
        tempLightObj.type = LightType.Directional;
        tempLightObj.intensity = 1.1f;

        Color oldAmbient = RenderSettings.ambientLight;
        RenderSettings.ambientLight = Color.white;

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolderPath });
        int processedCount = 0;

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null) continue;

            GameObject instance = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.Euler(0f, YawOffset, 0f);

            Bounds bounds = CalculateShoeBounds(instance, out bool hasRenderer);
            if (!hasRenderer)
            {
                bounds = new Bounds(instance.transform.position, Vector3.one);
            }

            Vector3 center = bounds.center;
            Vector3 size = bounds.size;

            // Góc nhìn hơi nghiêng 3/4 từ phía trước trên cao để thấy rõ cả đôi giày
            Vector3 viewDir = new Vector3(0.25f, 0.25f, -1f).normalized;
            float camDistance = Mathf.Max(size.x, size.y, size.z) * 3f + 2f;

            capCam.transform.position = center + viewDir * camDistance;
            capCam.transform.LookAt(center);
            tempLightObj.transform.position = capCam.transform.position;
            tempLightObj.transform.LookAt(center);

            float orthoSize = CalculateFittedOrthoSize(capCam, bounds, Padding);
            capCam.orthographicSize = Mathf.Max(orthoSize, 0.1f);

            RenderTexture rt = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32);
            capCam.targetTexture = rt;
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D screenShot = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            capCam.Render();
            screenShot.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
            screenShot.Apply();

            RenderTexture.active = previousActive;
            capCam.targetTexture = null;

            string iconPath = $"{OutputIconFolder}/{prefab.name}.png";
            File.WriteAllBytes(iconPath, screenShot.EncodeToPNG());

            DestroyImmediate(rt);
            DestroyImmediate(screenShot);

            // Reset rotation về 0 trước khi lưu prefab vật lý
            instance.transform.rotation = Quaternion.identity;
            Bounds prefabBounds = CalculateShoeBounds(instance, out bool hasPrefabRenderer);
            if (!hasPrefabRenderer)
            {
                prefabBounds = new Bounds(instance.transform.position, Vector3.one);
            }

            SetupPhysicsAndWorldItem(instance, prefabBounds);

            string outputPrefabPath = $"{WorldItemPrefabFolder}/{prefab.name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(instance, outputPrefabPath);

            DestroyImmediate(instance);
            processedCount++;
        }

        RenderSettings.ambientLight = oldAmbient;
        DestroyImmediate(tempLightObj.gameObject);
        DestroyImmediate(camObj);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Hoàn tất xử lý {processedCount} prefab giày tại '{PrefabFolderPath}'.");
    }

    private static Bounds CalculateShoeBounds(GameObject root, out bool foundAny)
    {
        foundAny = false;
        Bounds combined = new Bounds(Vector3.zero, Vector3.zero);

        Renderer[] allRenderers = root.GetComponentsInChildren<Renderer>(true);
        bool hasLod0 = false;

        foreach (Renderer r in allRenderers)
        {
            if (r is MeshRenderer || r is SkinnedMeshRenderer)
            {
                if (r.gameObject.name.ToLower().Contains("lod0"))
                {
                    hasLod0 = true;
                    break;
                }
            }
        }

        foreach (Renderer r in allRenderers)
        {
            if (!(r is MeshRenderer || r is SkinnedMeshRenderer)) continue;
            if (!r.gameObject.activeInHierarchy) continue;

            string lowerName = r.gameObject.name.ToLower();
            if (hasLod0 && (lowerName.Contains("lod1") || lowerName.Contains("lod2") || lowerName.Contains("lod3") || lowerName.Contains("lod4")))
            {
                continue;
            }

            if (r.bounds.size.sqrMagnitude <= 0.000001f || r.bounds.size.magnitude > 50f) continue;

            if (!foundAny)
            {
                combined = r.bounds;
                foundAny = true;
            }
            else
            {
                combined.Encapsulate(r.bounds);
            }
        }

        return combined;
    }

    private static float CalculateFittedOrthoSize(Camera cam, Bounds bounds, float padding)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        Vector3[] corners = new Vector3[8]
        {
            new Vector3(min.x, min.y, min.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(min.x, max.y, max.z),
            new Vector3(max.x, max.y, max.z)
        };

        Matrix4x4 worldToCam = cam.transform.worldToLocalMatrix;
        float maxX = 0f;
        float maxY = 0f;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 localPos = worldToCam.MultiplyPoint3x4(corners[i]);
            maxX = Mathf.Max(maxX, Mathf.Abs(localPos.x));
            maxY = Mathf.Max(maxY, Mathf.Abs(localPos.y));
        }

        float aspect = cam.aspect > 0f ? cam.aspect : 1f;
        float sizeFromHeight = maxY;
        float sizeFromWidth = maxX / aspect;

        return Mathf.Max(sizeFromHeight, sizeFromWidth) * padding;
    }

    private static void SetupPhysicsAndWorldItem(GameObject instance, Bounds worldBounds)
    {
        Rigidbody rb = instance.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = instance.AddComponent<Rigidbody>();
        }
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        BoxCollider[] existingColliders = instance.GetComponents<BoxCollider>();

        BoxCollider solidCollider = null;
        foreach (BoxCollider col in existingColliders)
        {
            if (!col.isTrigger)
            {
                solidCollider = col;
                break;
            }
        }
        if (solidCollider == null)
        {
            solidCollider = instance.AddComponent<BoxCollider>();
        }
        solidCollider.isTrigger = false;
        solidCollider.center = instance.transform.InverseTransformPoint(worldBounds.center);
        solidCollider.size = worldBounds.size;

        BoxCollider triggerCollider = null;
        foreach (BoxCollider col in existingColliders)
        {
            if (col.isTrigger && col != solidCollider)
            {
                triggerCollider = col;
                break;
            }
        }
        if (triggerCollider == null)
        {
            triggerCollider = instance.AddComponent<BoxCollider>();
        }
        triggerCollider.isTrigger = true;
        triggerCollider.center = solidCollider.center;
        triggerCollider.size = worldBounds.size * 2.5f;

        if (instance.GetComponent<WorldItem>() == null)
        {
            instance.AddComponent<WorldItem>();
        }
    }
}
#endif