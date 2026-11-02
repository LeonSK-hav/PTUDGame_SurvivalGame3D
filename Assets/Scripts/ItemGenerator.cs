using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class ItemGenerator
{
    [MenuItem("Tools/Tạo Item Tự Động Từ Tên Icon")]
    public static void GenerateItemsFromIcons()
    {
        string iconFolderPath = "Assets/GeneratedIcons"; 
        string itemOutputFolder = "Assets/Items";
        // Thư mục chứa các Prefab 3D của bạn (hãy đổi đường dẫn nếu cần)
        string prefabFolderPath = "Assets/Prefab"; // Hoặc thư mục chứa prefab của bạn
        //string prefabFolderPath = "Assets/ForEnviroment/Polygon-Lite Survival Collection/Prefabs"; // Hoặc thư mục chứa prefab của bạn
       //string prefabFolderPath = "Assets/ForPlayer/SSG_Guns_Pack_01_Demo/Prefabs"; // Thư mục chứa prefab của bạn
        if (!AssetDatabase.IsValidFolder(itemOutputFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Items");
        }

        string fullPath = Path.Combine(Application.dataPath, "GeneratedIcons");
        if (!Directory.Exists(fullPath))
        {
            Debug.LogError($"Không tìm thấy thư mục: {fullPath}");
            return;
        }

        string[] pngFiles = Directory.GetFiles(fullPath, "*.png");

        if (pngFiles.Length == 0)
        {
            Debug.LogWarning("Không tìm thấy file ảnh .png nào trong thư mục GeneratedIcons!");
            return;
        }

        foreach (string file in pngFiles)
        {
            string relativePath = "Assets" + file.Substring(Application.dataPath.Length).Replace("\\", "/");
            
            // Cấu hình lại TextureImporter để ép về dạng Sprite Single chuẩn xác
            TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
            if (importer != null)
            {
                if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.SaveAndReimport();
                }
            }

            // Quét tất cả các asset con bên trong file ảnh để lấy Sprite
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(relativePath);
            foreach (var asset in allAssets)
            {
                if (asset is Sprite iconSprite)
                {
                    string itemName = iconSprite.name;
                    string assetPath = $"{itemOutputFolder}/{itemName}.asset";
                    
                    ItemData existingItem = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);

                    // Tự động tìm Prefab ngoài thế giới tương ứng theo tên (hoặc dùng Prefab mặc định)
                    GameObject assignedPrefab = null;
                    if (AssetDatabase.IsValidFolder(prefabFolderPath))
                    {
                        // Cách 1: Tìm prefab có tên trùng khớp với Item (Ví dụ: Item tên "HealthPotion" sẽ tìm prefab tên "HealthPotion" hoặc "WorldItem_HealthPotion")
                        string prefabPath = $"{prefabFolderPath}/{itemName}.prefab";
                        assignedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                        // Nếu không tìm thấy prefab trùng tên, bạn có thể trỏ về Prefab mặc định chung (Ví dụ: WorldItem_Prefab)
                        if (assignedPrefab == null)
                        {
                            string defaultPrefabPath = $"{prefabFolderPath}/WorldItem_Prefab.prefab";
                            assignedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(defaultPrefabPath);
                        }
                    }

                    if (existingItem == null)
                    {
                        // Tạo mới ItemData
                        ItemData newItem = ScriptableObject.CreateInstance<ItemData>();
                        newItem.itemName = itemName;
                        newItem.itemIcon = iconSprite;
                        newItem.worldItemPrefab = assignedPrefab; // Gán Prefab vào đây
                        
                        // Thông tin mặc định
                        newItem.itemType = ItemType.Consumable; 
                        newItem.maxStack = 99;                  
                        newItem.healAmount = 25f;               
                        newItem.damageBonus = 0f;               
                        newItem.defenseBonus = 0f;
                        newItem.agilityBonus = 0f;

                        AssetDatabase.CreateAsset(newItem, assetPath);
                        Debug.Log($"Đã tạo thành công Item: {itemName} (Đã gán Prefab: {(assignedPrefab != null ? assignedPrefab.name : "Không tìm thấy")})");
                    }
                    else
                    {
                        bool isDirty = false;

                        if (existingItem.itemIcon == null)
                        {
                            existingItem.itemIcon = iconSprite;
                            isDirty = true;
                        }

                        // Nếu item cũ chưa có prefab thì tự động cập nhật gán vào
                        if (existingItem.worldItemPrefab == null && assignedPrefab != null)
                        {
                            existingItem.worldItemPrefab = assignedPrefab;
                            isDirty = true;
                        }

                        if (isDirty)
                        {
                            EditorUtility.SetDirty(existingItem);
                        }
                    }
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Hoàn tất quét ảnh, tạo ItemData và gán Prefab!");
    }
}
#endif