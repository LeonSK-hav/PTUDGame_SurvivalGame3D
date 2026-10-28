using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MyIndieGames.FantasticUI.Editor
{
    public abstract class MyIndieHubWindow : EditorWindow
    {
        public const string Publisher = "MyIndie.Games";

        int tab;
        Vector2 scroll;
        GUIStyle titleStyle;
        GUIStyle wrapStyle;
        GUIStyle sectionStyle;

        protected abstract string ProductName { get; }

        protected abstract string Version { get; }

        protected abstract string[] Tabs { get; }

        protected abstract void DrawTab(int index);

        protected GUIStyle WrapStyle => wrapStyle;

        public int Tab
        {
            get => tab;
            set => tab = Mathf.Clamp(value, 0, Tabs.Length - 1);
        }

        protected static T Open<T>(string title) where T : MyIndieHubWindow
        {
            var window = GetWindow<T>(false, title, true);
            window.minSize = new Vector2(440f, 520f);
            window.Show();
            return window;
        }

        protected static void OpenOnFirstImport(string productKey, System.Action open)
        {
            if (Application.isBatchMode)
            {
                return;
            }

            var key = "MyIndieGames." + productKey + ".HubShown." + Application.dataPath.GetHashCode();
            if (EditorPrefs.GetBool(key, false))
            {
                return;
            }

            EditorApplication.delayCall += () =>
            {
                EditorPrefs.SetBool(key, true);
                open();
            };
        }

        protected virtual void OnEnable()
        {
            Selection.selectionChanged += Repaint;
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        protected virtual void OnDisable()
        {
            Selection.selectionChanged -= Repaint;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
        }

        void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            Repaint();
        }

        protected virtual void OnGUI()
        {
            EnsureStyles();
            using (new EditorGUILayout.VerticalScope(sectionStyle))
            {
                EditorGUILayout.LabelField(ProductName, titleStyle, GUILayout.Height(28f));
                EditorGUILayout.LabelField("by " + Publisher + "  ·  v" + Version, EditorStyles.miniLabel);
            }

            tab = GUILayout.Toolbar(Mathf.Clamp(tab, 0, Tabs.Length - 1), Tabs, GUILayout.Height(26f));
            EditorGUILayout.Space(6f);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawTab(tab);
            EditorGUILayout.EndScrollView();
        }

        void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 20 };
            wrapStyle = new GUIStyle(EditorStyles.label) { wordWrap = true, richText = true };
            sectionStyle = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(10, 10, 8, 8) };
        }

        protected void Section(string title, System.Action body)
        {
            using (new EditorGUILayout.VerticalScope(sectionStyle))
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                EditorGUILayout.Space(2f);
                body();
            }

            EditorGUILayout.Space(6f);
        }

        protected void Paragraph(string text)
        {
            EditorGUILayout.LabelField(text, wrapStyle);
        }

        protected void Steps(params string[] steps)
        {
            for (var i = 0; i < steps.Length; i++)
            {
                EditorGUILayout.LabelField("<b>" + (i + 1) + ".</b> " + steps[i], wrapStyle);
            }
        }

        protected static bool Button(string label, float height = 0f)
        {
            return height > 0f ? GUILayout.Button(label, GUILayout.Height(height)) : GUILayout.Button(label);
        }

        protected static void Check(bool ok, string label, string detail)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var icon = EditorGUIUtility.IconContent(ok ? "TestPassed" : "console.warnicon.sml");
                GUILayout.Label(icon, GUILayout.Width(20f), GUILayout.Height(18f));
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(detail, EditorStyles.wordWrappedMiniLabel);
                }
            }

            EditorGUILayout.Space(2f);
        }

        protected static bool PipelineCheck(out bool isUniversal)
        {
            var pipeline = GraphicsSettings.currentRenderPipeline;
            var name = pipeline == null ? "Built-in Render Pipeline" : pipeline.GetType().Name;
            var isHdrp = pipeline != null && name.Contains("HDRenderPipeline");
            isUniversal = pipeline != null && name.Contains("Universal");
            Check(!isHdrp, "Render pipeline: " + (isUniversal ? "URP" : pipeline == null ? "Built-in" : name),
                isHdrp ? "HDRP is not supported. Use URP or the Built-in pipeline." : "Supported.");
            return !isHdrp;
        }

        protected static void ColorSpaceCheck(string reason = "Linear gives correct colors and lighting.")
        {
            var linear = PlayerSettings.colorSpace == ColorSpace.Linear;
            Check(linear, "Color space: " + PlayerSettings.colorSpace, linear ? "Recommended setting." : reason);
            if (!linear && GUILayout.Button("Switch To Linear"))
            {
                if (EditorUtility.DisplayDialog("Color Space", "Switch the project to Linear color space? Unity will reimport textures.", "Switch", "Cancel"))
                {
                    PlayerSettings.colorSpace = ColorSpace.Linear;
                }
            }
        }

        protected void ModelGrid(IReadOnlyList<(GameObject prefab, string label)> models, System.Action<GameObject> onClick, float cell = 128f)
        {
            var columns = Mathf.Max(1, Mathf.FloorToInt((position.width - 40f) / (cell + 6f)));
            for (var i = 0; i < models.Count; i += columns)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (var c = i; c < Mathf.Min(i + columns, models.Count); c++)
                    {
                        ModelButton(models[c].prefab, models[c].label, cell, onClick);
                    }

                    GUILayout.FlexibleSpace();
                }
            }
        }

        void ModelButton(GameObject prefab, string label, float size, System.Action<GameObject> onClick)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(size)))
            {
                var preview = prefab != null ? AssetPreview.GetAssetPreview(prefab) : null;
                if (preview == null && prefab != null && AssetPreview.IsLoadingAssetPreviews())
                {
                    Repaint();
                }

                var content = preview != null ? new GUIContent(preview, "Add " + label) : new GUIContent(label, "Add " + label);
                using (new EditorGUI.DisabledScope(prefab == null))
                {
                    if (GUILayout.Button(content, GUILayout.Width(size), GUILayout.Height(size)))
                    {
                        onClick(prefab);
                    }
                }

                EditorGUILayout.LabelField(label, EditorStyles.centeredGreyMiniLabel, GUILayout.Width(size));
            }
        }

        protected static GameObject PlaceAtPivot(GameObject prefab, bool snapToTerrain = true)
        {
            if (prefab == null)
            {
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var view = SceneView.lastActiveSceneView;
            var point = view != null ? view.pivot : Vector3.zero;
            var terrain = Terrain.activeTerrain;
            if (snapToTerrain && terrain != null)
            {
                point.y = terrain.SampleHeight(point) + terrain.transform.position.y;
            }

            instance.transform.position = point;
            Undo.RegisterCreatedObjectUndo(instance, "Add " + prefab.name);
            Selection.activeGameObject = instance;
            return instance;
        }

        protected static GameObject FindPrefab(string name, string folder = null)
        {
            var folders = string.IsNullOrEmpty(folder) ? null : new[] { folder };
            foreach (var guid in AssetDatabase.FindAssets(name + " t:Prefab", folders))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == name)
                {
                    return AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
            }

            return null;
        }

        protected static void OpenScene(string sceneName)
        {
            var guid = AssetDatabase.FindAssets(sceneName + " t:Scene").FirstOrDefault();
            if (guid == null)
            {
                EditorUtility.DisplayDialog(Publisher, "Scene '" + sceneName + "' was not found.", "OK");
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(guid));
            }
        }

        protected static void Ping(Object asset)
        {
            if (asset == null)
            {
                return;
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        protected static IEnumerable<T> FindAssets<T>(string folder = null) where T : Object
        {
            var folders = string.IsNullOrEmpty(folder) ? null : new[] { folder };
            return AssetDatabase.FindAssets("t:" + typeof(T).Name, folders)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .Where(asset => asset != null);
        }

        protected static T CreateAsset<T>(string title, string defaultName) where T : ScriptableObject
        {
            var path = EditorUtility.SaveFilePanelInProject(title, defaultName, "asset", "Choose where to save the asset.");
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            var asset = CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Ping(asset);
            return asset;
        }

        protected static IEnumerable<T> AllInOpenScenes<T>() where T : Component
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                {
                    continue;
                }

                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var component in root.GetComponentsInChildren<T>(true))
                    {
                        yield return component;
                    }
                }
            }
        }

        protected static List<T> SelectedComponents<T>() where T : Component
        {
            return Selection.gameObjects
                .SelectMany(go => go.GetComponentsInChildren<T>(true))
                .Distinct()
                .ToList();
        }

        protected static void ForEach<T>(IEnumerable<T> targets, string undoName, System.Action<T> action) where T : Object
        {
            foreach (var target in targets)
            {
                Undo.RecordObject(target, undoName);
                action(target);
                EditorUtility.SetDirty(target);
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            }
        }
    }
}
