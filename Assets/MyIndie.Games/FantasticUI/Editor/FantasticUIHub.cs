using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MyIndieGames.FantasticUI.Editor
{
    public class FantasticUIHub : MyIndieHubWindow
    {
        public const string ProductVersion = "2.0.0";

        static readonly string[] TabNames = { "Start", "Setup", "Create", "Sprites" };

        static readonly string[] PrefabGroups = { "Windows", "Buttons", "Bars", "Slots", "Widgets" };

        string spriteCategory = "Panels";
        string spriteFilter = string.Empty;

        protected override string ProductName => "Fantastic UI Starter Pack";

        protected override string Version => ProductVersion;

        protected override string[] Tabs => TabNames;

        [MenuItem("Tools/MyIndie.Games/Fantastic UI/Fantastic UI Hub", false, 0)]
        [MenuItem("Window/MyIndie.Games/Fantastic UI Hub")]
        public static void Open()
        {
            Open<FantasticUIHub>("Fantastic UI");
        }

        [InitializeOnLoadMethod]
        static void OpenOnFirstImport()
        {
            OpenOnFirstImport("FantasticUI", Open);
        }

        public static void OpenSceneAt(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            {
                EditorUtility.DisplayDialog(Publisher, "Scene not found:\n" + path, "OK");
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(path);
            }
        }

        protected override void DrawTab(int index)
        {
            switch (index)
            {
                case 0:
                    DrawStart();
                    break;
                case 1:
                    DrawSetup();
                    break;
                case 2:
                    DrawCreate();
                    break;
                default:
                    DrawSprites();
                    break;
            }
        }

        void DrawStart()
        {
            Section("Welcome", () =>
            {
                Paragraph("A free fantasy UI kit: parchment, wood and stone panels, buttons, bars, inventory slots and more than 50 item icons, with ready prefabs and small scripts for windows, bars, drag and drop slots and tooltips. Works with uGUI and TextMeshPro in Built-in, URP and HDRP.");
            });

            Section("Quick start", () =>
            {
                Steps(
                    "Check the <b>Setup</b> tab. TextMeshPro Essentials must be imported once per project.",
                    "Open the <b>Demo</b> scene and press Play to see every piece working together.",
                    "Add windows, buttons, bars and slots from the <b>Create</b> tab or from <b>GameObject > MyIndie.Games > Fantastic UI</b>.",
                    "Pick any sprite in the <b>Sprites</b> tab to put it on the selected Image.");
            });

            Section("Samples", () =>
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (Button("Open Demo", 30f))
                    {
                        OpenSceneAt(FantasticUIEditorUtility.DemoScene);
                    }

                    if (Button("Open Sprite Gallery", 30f))
                    {
                        OpenSceneAt(FantasticUIEditorUtility.GalleryScene);
                    }
                }
            });

            Section("Documentation", () =>
            {
                Paragraph("The user guide is in the Documentation folder.");
                if (Button("Show Documentation"))
                {
                    var guide = AssetDatabase.LoadAssetAtPath<Object>(FantasticUIEditorUtility.RootFolder + "/Documentation/FantasticUI_Documentation.pdf")
                                ?? AssetDatabase.LoadAssetAtPath<Object>(FantasticUIEditorUtility.RootFolder + "/Documentation/FantasticUI_Documentation.md");
                    Ping(guide);
                }
            });
        }

        void DrawSetup()
        {
            Section("Project checks", () =>
            {
                var essentials = FantasticUIEditorUtility.EssentialsImported;
                Check(essentials, "TextMeshPro Essentials", essentials ? "Imported." : "Needed for every label in the prefabs and samples.");
                if (!essentials && Button("Import TMP Essentials", 26f))
                {
                    FantasticUIEditorUtility.ImportEssentials();
                }

                var prefabs = AssetDatabase.IsValidFolder(FantasticUIEditorUtility.PrefabsFolder);
                Check(prefabs, "Prefabs", prefabs ? FantasticUIEditorUtility.PrefabsFolder : "Prefabs folder is missing. Re-import the package.");
                var sprites = PlayerSettingsSpritePacker();
                Check(sprites, "Sprite Atlas", sprites ? "Sprite Atlas packing is on, the UI draws in very few batches." : "Turn on Sprite Atlas in Project Settings > Editor > Sprite Packer for fewer draw calls.");
            });

            Section("Input", () =>
            {
                Paragraph("Fantastic Event System picks the right UI input module at runtime: Input System UI Input Module when the Input System package is active, Standalone Input Module otherwise. Put it on your EventSystem, or use the menu items, which add it for you.");
            });
        }

        static bool PlayerSettingsSpritePacker()
        {
            return EditorSettings.spritePackerMode == SpritePackerMode.AlwaysOnAtlas || EditorSettings.spritePackerMode == SpritePackerMode.BuildTimeOnlyAtlas;
        }

        void DrawCreate()
        {
            Paragraph("Click to add to the selected canvas. A canvas and an event system are created when the scene has none.");
            EditorGUILayout.Space(4f);
            foreach (var group in PrefabGroups)
            {
                var prefabs = FantasticUIEditorUtility.AllPrefabs(group).Where(p => p.name != "FantasticUI Canvas").ToList();
                if (prefabs.Count == 0)
                {
                    continue;
                }

                Section(group, () =>
                {
                    ModelGrid(prefabs.Select(p => (p, p.name.Replace('_', ' '))).ToList(), p => FantasticUIEditorUtility.Create(p), 96f);
                });
            }
        }

        void DrawSprites()
        {
            var catalog = FindAssets<FantasticSpriteCatalog>(FantasticUIEditorUtility.RootFolder).FirstOrDefault();
            if (catalog == null)
            {
                Paragraph("Sprite catalog not found.");
                return;
            }

            var categories = catalog.Categories();
            var current = Mathf.Max(0, categories.IndexOf(spriteCategory));
            current = GUILayout.SelectionGrid(current, categories.ToArray(), 4);
            spriteCategory = categories.Count > 0 ? categories[current] : spriteCategory;
            spriteFilter = EditorGUILayout.TextField("Filter", spriteFilter);
            var image = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<Image>() : null;
            Paragraph(image != null ? "Click a sprite to put it on <b>" + image.name + "</b>." : "Select a UI Image to assign sprites, or click to create a new Image.");
            EditorGUILayout.Space(4f);

            var entries = catalog.InCategory(spriteCategory)
                .Where(e => e.sprite != null && (string.IsNullOrEmpty(spriteFilter) || e.name.IndexOf(spriteFilter, System.StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();
            const float cell = 84f;
            var columns = Mathf.Max(1, Mathf.FloorToInt((position.width - 40f) / (cell + 6f)));
            for (var i = 0; i < entries.Count; i += columns)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (var c = i; c < Mathf.Min(i + columns, entries.Count); c++)
                    {
                        DrawSprite(entries[c], image, cell);
                    }

                    GUILayout.FlexibleSpace();
                }
            }
        }

        void DrawSprite(FantasticSpriteCatalog.Entry entry, Image image, float size)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(size)))
            {
                var preview = AssetPreview.GetAssetPreview(entry.sprite);
                if (preview == null && AssetPreview.IsLoadingAssetPreviews())
                {
                    Repaint();
                }

                var content = preview != null ? new GUIContent(preview, entry.name) : new GUIContent(entry.name, entry.name);
                if (GUILayout.Button(content, GUILayout.Width(size), GUILayout.Height(size)))
                {
                    Apply(entry.sprite, image);
                }

                EditorGUILayout.LabelField(entry.name.Replace('_', ' '), EditorStyles.centeredGreyMiniLabel, GUILayout.Width(size));
            }
        }

        static void Apply(Sprite sprite, Image image)
        {
            var sliced = sprite.border.sqrMagnitude > 0f;
            if (image == null)
            {
                var canvas = FantasticUIEditorUtility.EnsureCanvas();
                var parent = Selection.activeTransform != null && Selection.activeTransform.GetComponentInParent<Canvas>() != null ? Selection.activeTransform : canvas.transform;
                var created = new GameObject(sprite.name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)) { layer = 5 };
                created.transform.SetParent(parent, false);
                Undo.RegisterCreatedObjectUndo(created, "Create " + sprite.name);
                image = created.GetComponent<Image>();
                image.sprite = sprite;
                image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
                image.preserveAspect = !sliced;
                image.SetNativeSize();
                Selection.activeGameObject = created;
                return;
            }

            Undo.RecordObject(image, "Assign Sprite");
            image.sprite = sprite;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = !sliced;
            EditorUtility.SetDirty(image);
            PrefabUtility.RecordPrefabInstancePropertyModifications(image);
        }
    }
}
