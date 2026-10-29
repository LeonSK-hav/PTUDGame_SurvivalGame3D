using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class PrefabToIconGenerator : MonoBehaviour
{
    [MenuItem("Tools/Tạo Icon 2D (Hoàn chỉnh, không bị cắt)")]
    public static void GenerateIcons()
    {
         // Thư mục chứa các Prefab 3D của bạn (hãy đổi đường dẫn nếu cần)
        string prefabFolderPath = "Assets/ForPlayer/Low Poly Weapons VOL.1/Prefabs"; // Hoặc thư mục chứa prefab của bạn
        //string prefabFolderPath = "Assets/ForEnviroment/Polygon-Lite Survival Collection/Prefabs"; // Hoặc thư mục chứa prefab của bạn
       //string prefabFolderPath = "Assets/ForPlayer/SSG_Guns_Pack_01_Demo/Prefabs"; // Thư mục chứa prefab của bạn
        string outputFolder = "Assets/GeneratedIcons";

        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            AssetDatabase.CreateFolder("Assets", "GeneratedIcons");
        }

        // Tạo Camera ẩn
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
                // Sinh bản thể tạm
                GameObject objInstance = Instantiate(prefab, Vector3.zero, Quaternion.identity);

                // Lọc các MeshRenderer chính, bỏ qua các vật thể quá khổ (địa hình thừa)
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

                Vector3 center = mainBounds.center;
                float maxDimension = Mathf.Max(mainBounds.size.x, mainBounds.size.y, mainBounds.size.z);
                
                // --- ĐIỀU CHỈNH KHOẢNG CÁCH VÀ GÓC NHÌN AN TOÀN ---
                float distance = maxDimension * 2.5f + 1.0f;
                Vector3 offset = new Vector3(distance * 0.6f, distance * 0.35f, -distance * 0.6f);
                
                capCam.transform.position = center + offset;
                capCam.transform.LookAt(center);
                
                // TĂNG KÍCH THƯỚC KHUNG NHÌN LÊN ĐỂ KHÔNG BỊ CẮT LẸM CẠNH
                capCam.orthographicSize = (maxDimension * 0.7f); 

                // Render và chụp ảnh
                RenderTexture rt = new RenderTexture(256, 256, 24);
                capCam.targetTexture = rt;
                Texture2D screenShot = new Texture2D(256, 256, TextureFormat.RGBA32, false);

                capCam.Render();
                RenderTexture.active = rt;
                screenShot.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
                screenShot.Apply();

                // Lưu file PNG
                byte[] bytes = screenShot.EncodeToPNG();
                string savePath = $"{outputFolder}/{prefab.name}.png";
                File.WriteAllBytes(savePath, bytes);

                DestroyImmediate(objInstance);
                DestroyImmediate(rt);
            }
        }

        DestroyImmediate(camObj);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Đã tạo lại toàn bộ icon trọn vẹn, không bị cắt lẹm!");
    }
}
#endif