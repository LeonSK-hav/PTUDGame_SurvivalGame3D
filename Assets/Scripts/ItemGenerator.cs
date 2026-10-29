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

                    if (existingItem == null)
                    {
                        // Tạo mới ItemData
                        ItemData newItem = ScriptableObject.CreateInstance<ItemData>();
                        newItem.itemName = itemName;
                        newItem.itemIcon = iconSprite;
                        
                        // Thông tin mặc định
                        newItem.itemType = ItemType.Consumable; 
                        newItem.itemUsageDescription = $"Vật phẩm sinh tồn: {itemName}.";
                        newItem.maxStack = 99;                  
                        newItem.healAmount = 25f;               
                        newItem.damageBonus = 0f;               

                        AssetDatabase.CreateAsset(newItem, assetPath);
                        Debug.Log($"Đã tạo thành công Item: {itemName}");
                    }
                    else
                    {
                        if (existingItem.itemIcon == null)
                        {
                            existingItem.itemIcon = iconSprite;
                            EditorUtility.SetDirty(existingItem);
                        }
                    }
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Hoàn tất quét ảnh và tạo toàn bộ ItemData!");
    }
}
#endif