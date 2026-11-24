#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public class CartridgeIconAndPhysicsGenerator : MonoBehaviour
{
    private const string SourceFolderPath = "Assets/9x39mm-spp-cartridge-high-poly/source/model/model";
    //private const string SourceFolderPath = "Assets/12g-ammo/source";
    private const string OutputIconFolder = "Assets/GeneratedIcons";
    private const string WorldItemPrefabFolder = "Assets/Prefab";

    private const float YawOffset = 0f;
    private const float Padding = 1.55f;
    private const int IconSize = 256;

    [MenuItem("Tools/Tạo Icon Đạn 9x39mm & Tự Động Thêm Vật Lý")]
    public static void GenerateCartridgeIconsAndPhysics()
    {
        if (!AssetDatabase.IsValidFolder(OutputIconFolder))
        {
            AssetDatabase.CreateFolder("Assets", "GeneratedIcons");
        }

        if (!AssetDatabase.IsValidFolder(WorldItemPrefabFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Prefab");
        }

        GameObject camObj = new GameObject("CartridgeIconCaptureCamera");
        Camera capCam = camObj.AddComponent<Camera>();
        capCam.clearFlags = CameraClearFlags.SolidColor;
        capCam.backgroundColor = new Color(0, 0, 0, 0);
        capCam.orthographic = true;
        capCam.nearClipPlane = 0.01f;
        capCam.farClipPlane = 200f;

        Light tempLightObj = new GameObject("CartridgeIconLight").AddComponent<Light>();
        tempLightObj.type = LightType.Directional;
        tempLightObj.intensity = 1.15f;

        Color oldAmbient = RenderSettings.ambientLight;
        RenderSettings.ambientLight = Color.white;

        string[] guids = AssetDatabase.FindAssets("t:Prefab t:Model", new[] { SourceFolderPath });
        int processedCount = 0;

        try
        {
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject sourceAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (sourceAsset == null) continue;

                GameObject instance = Instantiate(sourceAsset, Vector3.zero, Quaternion.identity);
                instance.transform.position = Vector3.zero;
                instance.transform.rotation = Quaternion.Euler(0f, YawOffset, 0f);

                Bounds bounds = CalculateCartridgeBounds(instance, out bool hasRenderer);
                if (!hasRenderer)
                {
                    bounds = new Bounds(instance.transform.position, Vector3.one * 0.2f);
                }

                Vector3 center = bounds.center;
                float maxExtent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);

                // Góc nhìn chếch 3/4 để thấy trọn cả đầu đạn lẫn vỏ đạn dài
                Vector3 viewDir = new Vector3(0.45f, 0.25f, -1f).normalized;
                float camDistance = Mathf.Max(maxExtent * 5f + 2f, 3f);

                capCam.nearClipPlane = 0.01f;
                capCam.farClipPlane = camDistance * 4f + 50f;
                capCam.transform.position = center + viewDir * camDistance;
                capCam.transform.LookAt(center);

                tempLightObj.transform.position = capCam.transform.position;
                tempLightObj.transform.LookAt(center);

                float orthoSize = CalculateFittedOrthoSize(capCam, bounds, Padding);
                capCam.orthographicSize = Mathf.Max(orthoSize, 0.05f);

                string safeName = string.IsNullOrEmpty(sourceAsset.name) || sourceAsset.name == "model"
                    ? "Ammo_9x39mm_SPP"
                    : sourceAsset.name;

                string iconPath = $"{OutputIconFolder}/{safeName}.png";
                CaptureIconToPng(capCam, iconPath);

                // Reset xoay về 0 trước khi gắn vật lý và lưu prefab
                instance.transform.rotation = Quaternion.identity;
                Bounds prefabBounds = CalculateCartridgeBounds(instance, out bool hasPrefabRenderer);
                if (!hasPrefabRenderer)
                {
                    prefabBounds = new Bounds(instance.transform.position, Vector3.one * 0.2f);
                }

                SetupPhysicsAndWorldItem(instance, prefabBounds);

                string outputPrefabPath = $"{WorldItemPrefabFolder}/{safeName}.prefab";
                PrefabUtility.SaveAsPrefabAsset(instance, outputPrefabPath);

                DestroyImmediate(instance);
                processedCount++;
            }
        }
        finally
        {
            RenderSettings.ambientLight = oldAmbient;
            if (tempLightObj != null) DestroyImmediate(tempLightObj.gameObject);
            if (camObj != null) DestroyImmediate(camObj);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Chuẩn hóa import setting của icon thành Sprite đơn (Single) trong suốt
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject sourceAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (sourceAsset == null) continue;

            string safeName = string.IsNullOrEmpty(sourceAsset.name) || sourceAsset.name == "model"
                ? "Ammo_9x39mm_SPP"
                : sourceAsset.name;
            ConfigureSpriteImporter($"{OutputIconFolder}/{safeName}.png");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Hoàn tất xử lý {processedCount} asset đạn tại '{SourceFolderPath}'.");
    }

    private static void CaptureIconToPng(Camera capCam, string iconPath)
    {
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture rt = new RenderTexture(IconSize, IconSize, 24, RenderTextureFormat.ARGB32);
        Texture2D screenShot = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);

        try
        {
            capCam.targetTexture = rt;
            RenderTexture.active = rt;

            capCam.Render();
            screenShot.ReadPixels(new Rect(0, 0, IconSize, IconSize), 0, 0);
            screenShot.Apply();

            File.WriteAllBytes(iconPath, screenShot.EncodeToPNG());
        }
        finally
        {
            if (capCam != null && capCam.targetTexture == rt)
            {
                capCam.targetTexture = null;
            }
            if (RenderTexture.active == rt)
            {
                RenderTexture.active = previousActive;
            }
            if (rt != null)
            {
                rt.Release();
                DestroyImmediate(rt);
            }
            if (screenShot != null)
            {
                DestroyImmediate(screenShot);
            }
        }
    }

    private static void ConfigureSpriteImporter(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }

    private static Bounds CalculateCartridgeBounds(GameObject root, out bool foundAny)
    {
        foundAny = false;
        Bounds combined = new Bounds(Vector3.zero, Vector3.zero);

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            if (!(r is MeshRenderer || r is SkinnedMeshRenderer)) continue;
            if (!r.gameObject.activeInHierarchy) continue;
            if (r.bounds.size.sqrMagnitude <= 0.0000001f || r.bounds.size.magnitude > 100f) continue;

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
        return Mathf.Max(maxY, maxX / aspect) * padding;
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
        solidCollider.size = Vector3.Max(worldBounds.size, Vector3.one * 0.05f);

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
        triggerCollider.size = solidCollider.size * 2.5f;

        if (instance.GetComponent<WorldItem>() == null)
        {
            instance.AddComponent<WorldItem>();
        }
    }
}
#endif