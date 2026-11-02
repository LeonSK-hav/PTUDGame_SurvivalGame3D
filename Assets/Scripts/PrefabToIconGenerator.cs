using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class PrefabToIconGenerator : MonoBehaviour
{
    [MenuItem("Tools/Tạo Icon & Tự Động Thêm Component Vật Lý Cho Prefab")]
    public static void GenerateIconsAndPhysics()
    {
        // Thư mục chứa các Prefab 3D của bạn
        string prefabFolderPath = "Assets/ForPlayer/Low Poly Weapons VOL.1/Prefabs"; 
        string outputFolder = "Assets/GeneratedIcons";
        string worldItemPrefabFolder = "Assets/Prefab"; // Thư mục lưu prefab dùng ngoài thế giới sau khi đã được gắn vật lý

        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            AssetDatabase.CreateFolder("Assets", "GeneratedIcons");
        }

        if (!AssetDatabase.IsValidFolder(worldItemPrefabFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Prefab");
        }

        // Tạo Camera ẩn để chụp icon
        GameObject camObj = new GameObject("IconCaptureCamera");
        Camera capCam = camObj.AddComponent<Camera>();
        capCam.clearFlags = CameraClearFlags.SolidColor;
        capCam.backgroundColor = new Color(0, 0, 0, 0); // Nền trong suốt
        capCam.orthographic = true;

        RenderSettings.ambientLight = Color.white;

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolderPath });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null)
            {
                // 1. SINH BẢN THỂ TẠM ĐỂ CHỤP ICON VÀ TÍNH TOÁN KÍCH THƯỚC
                GameObject objInstance = Instantiate(prefab, Vector3.zero, Quaternion.identity);

                MeshRenderer[] renderers = objInstance.GetComponentsInChildren<MeshRenderer>();
                Bounds mainBounds = new Bounds(Vector3.zero, Vector3.zero);
                bool initialized = false;

                foreach (MeshRenderer mr in renderers)
                {
                    if (mr.bounds.size.magnitude > 50f) continue; // Bỏ qua sàn/địa hình quá lớn

                    if (!initialized)
                    {
                        mainBounds = mr.bounds;
                        initialized = true;
                    }
                    else
                    {
                        mainBounds.Encapsulate(mr.bounds);
                    }
                }

                if (!initialized) mainBounds = new Bounds(objInstance.transform.position, Vector3.one);

                // --- CHỤP ẢNH ICON 2D ---
                Vector3 center = mainBounds.center;
                float maxDimension = Mathf.Max(mainBounds.size.x, mainBounds.size.y, mainBounds.size.z);
                
                float distance = maxDimension * 2.5f + 1.0f;
                Vector3 offset = new Vector3(distance * 0.6f, distance * 0.35f, -distance * 0.6f);
                
                capCam.transform.position = center + offset;
                capCam.transform.LookAt(center);
                capCam.orthographicSize = (maxDimension * 0.7f); 

                RenderTexture rt = new RenderTexture(256, 256, 24);
                capCam.targetTexture = rt;
                Texture2D screenShot = new Texture2D(256, 256, TextureFormat.RGBA32, false);

                capCam.Render();
                RenderTexture.active = rt;
                screenShot.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
                screenShot.Apply();

                byte[] bytes = screenShot.EncodeToPNG();
                string savePath = $"{outputFolder}/{prefab.name}.png";
                File.WriteAllBytes(savePath, bytes);

                DestroyImmediate(rt);

                // --- 2. TỰ ĐỘNG BỔ SUNG RIGIDBODY, 2 BOX COLLIDER VÀ SCRIPT WORLDITEM CHO PREFAB ---
                
                // Thêm Rigidbody
                Rigidbody rb = objInstance.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = objInstance.AddComponent<Rigidbody>();
                }
                rb.useGravity = true;
                rb.isKinematic = false;

                // --- BOX COLLIDER 1: Dùng để va chạm cứng với mặt đất (Không phải Trigger) ---
                // Lấy tất cả BoxCollider trên object, tìm cái nào không phải trigger hoặc tạo mới
                BoxCollider solidCollider = null;
                BoxCollider[] existingColliders = objInstance.GetComponents<BoxCollider>();
                foreach (var col in existingColliders)
                {
                    if (!col.isTrigger)
                    {
                        solidCollider = col;
                        break;
                    }
                }

                if (solidCollider == null)
                {
                    solidCollider = objInstance.AddComponent<BoxCollider>();
                }
                solidCollider.isTrigger = false; 
                solidCollider.center = objInstance.transform.InverseTransformPoint(mainBounds.center);
                solidCollider.size = mainBounds.size;

                // --- BOX COLLIDER 2: Dùng làm vùng Trigger lớn để dò/hút vật phẩm từ xa ---
                BoxCollider triggerCollider = null;
                foreach (var col in existingColliders)
                {
                    if (col.isTrigger && col != solidCollider)
                    {
                        triggerCollider = col;
                        break;
                    }
                }

                if (triggerCollider == null)
                {
                    triggerCollider = objInstance.AddComponent<BoxCollider>();
                }
                triggerCollider.isTrigger = true; // Bật chế độ Trigger để dò khoảng cách
                triggerCollider.center = solidCollider.center;
                // Phóng to kích thước lên gấp nhiều lần so với vật thể (ví dụ: nhân bản kính thước lên 3 hoặc 4 lần để làm vùng quét rộng)
                triggerCollider.size = mainBounds.size * 3.5f; 

                // Gắn luôn script WorldItem để sẵn sàng nhặt đồ
                WorldItem worldItemScript = objInstance.GetComponent<WorldItem>();
                if (worldItemScript == null)
                {
                    objInstance.AddComponent<WorldItem>();
                }

                // Lưu bản thể này thành một Prefab hoàn chỉnh trong thư mục Assets/Prefab
                string worldItemPrefabPath = $"{worldItemPrefabFolder}/{prefab.name}.prefab";
                PrefabUtility.SaveAsPrefabAsset(objInstance, worldItemPrefabPath);

                DestroyImmediate(objInstance);
            }
        }

        DestroyImmediate(camObj);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Đã hoàn tất: Tạo Icon 2D VÀ tự động gán Rigidbody, 1 Box Collider cứng để đứng đất + 1 Box Collider Trigger lớn để dò vật phẩm từ xa!");
    }
}
#endif