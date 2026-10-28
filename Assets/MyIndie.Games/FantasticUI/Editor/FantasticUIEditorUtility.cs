using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MyIndieGames.FantasticUI.Editor
{
    public static class FantasticUIEditorUtility
    {
        public static string RootFolder
        {
            get
            {
                var guids = AssetDatabase.FindAssets("FantasticWindow t:MonoScript");
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.EndsWith("/Runtime/FantasticWindow.cs"))
                    {
                        return path.Substring(0, path.Length - "/Runtime/FantasticWindow.cs".Length);
                    }
                }

                return "Assets/MyIndie.Games/FantasticUI";
            }
        }

        public static string PrefabsFolder => RootFolder + "/Prefabs";

        public static string DemoScene => RootFolder + "/Samples/Demo/FantasticUIDemo.unity";

        public static string GalleryScene => RootFolder + "/Samples/Gallery/FantasticUIGallery.unity";

        public static bool EssentialsImported => TMP_Settings.instance != null && TMP_Settings.defaultFontAsset != null;

        public static void ImportEssentials()
        {
            if (!EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources"))
            {
                Debug.LogWarning("[Fantastic UI] Import TMP Essential Resources from Window > TextMeshPro.");
            }
        }

        public static GameObject FindPrefab(string name)
        {
            var folder = PrefabsFolder;
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return null;
            }

            foreach (var guid in AssetDatabase.FindAssets(name + " t:Prefab", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == name)
                {
                    return AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
            }

            return null;
        }

        public static List<GameObject> AllPrefabs(string subfolder)
        {
            var folder = PrefabsFolder + "/" + subfolder;
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return new List<GameObject>();
            }

            return AssetDatabase.FindAssets("t:Prefab", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(p => p)
                .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                .ToList();
        }

        public static Canvas EnsureCanvas()
        {
            var selected = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<Canvas>() : null;
            if (selected != null)
            {
                return selected;
            }

            var existing = Object.FindAnyObjectByType<Canvas>();
            if (existing != null)
            {
                return existing.rootCanvas;
            }

            GameObject canvasObject;
            var canvasPrefab = FindPrefab("FantasticUI Canvas");
            if (canvasPrefab != null)
            {
                canvasObject = (GameObject)PrefabUtility.InstantiatePrefab(canvasPrefab);
            }
            else
            {
                canvasObject = new GameObject("FantasticUI Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            canvasObject.layer = 5;
            Undo.RegisterCreatedObjectUndo(canvasObject, "Create Fantastic UI Canvas");
            EnsureEventSystem();
            return canvasObject.GetComponent<Canvas>();
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(FantasticEventSystem));
            Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
        }

        public static GameObject Create(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogWarning("[Fantastic UI] Prefab not found. Keep the Prefabs folder next to the Runtime folder.");
                return null;
            }

            var canvas = EnsureCanvas();
            var parent = Selection.activeTransform != null && Selection.activeTransform.GetComponentInParent<Canvas>() != null ? Selection.activeTransform : canvas.transform;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            var rect = instance.transform as RectTransform;
            if (rect != null && prefab.name != "FantasticUI Canvas")
            {
                rect.anchoredPosition = Vector2.zero;
            }

            if (instance.GetComponent<FantasticTooltip>() == null && prefab.GetComponentInChildren<FantasticSlot>(true) != null)
            {
                EnsureTooltip(canvas);
            }

            Undo.RegisterCreatedObjectUndo(instance, "Create " + prefab.name);
            Selection.activeGameObject = instance;
            EditorSceneManager.MarkSceneDirty(instance.scene);
            return instance;
        }

        public static void EnsureTooltip(Canvas canvas)
        {
            if (canvas.GetComponentInChildren<FantasticTooltip>(true) != null)
            {
                return;
            }

            var prefab = FindPrefab("Tooltip");
            if (prefab == null)
            {
                return;
            }

            var tooltip = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
            tooltip.transform.SetAsLastSibling();
            Undo.RegisterCreatedObjectUndo(tooltip, "Create Tooltip");
        }
    }
}
