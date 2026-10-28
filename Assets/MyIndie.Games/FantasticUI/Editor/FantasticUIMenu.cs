using UnityEditor;
using UnityEngine;

namespace MyIndieGames.FantasticUI.Editor
{
    public static class FantasticUIMenu
    {
        const string Menu = "GameObject/MyIndie.Games/Fantastic UI/";
        const int Priority = 10;

        static void Create(string prefab)
        {
            FantasticUIEditorUtility.Create(FantasticUIEditorUtility.FindPrefab(prefab));
        }

        [MenuItem(Menu + "Canvas", false, Priority)]
        static void Canvas() => FantasticUIEditorUtility.EnsureCanvas();

        [MenuItem(Menu + "Windows/Parchment", false, Priority + 20)]
        static void WindowParchment() => Create("Window_Parchment");

        [MenuItem(Menu + "Windows/Wood", false, Priority + 21)]
        static void WindowWood() => Create("Window_Wood");

        [MenuItem(Menu + "Windows/Runestone", false, Priority + 22)]
        static void WindowRunestone() => Create("Window_Runestone");

        [MenuItem(Menu + "Windows/Emerald", false, Priority + 23)]
        static void WindowEmerald() => Create("Window_Emerald");

        [MenuItem(Menu + "Buttons/Wood", false, Priority + 40)]
        static void ButtonWood() => Create("Button_Wood");

        [MenuItem(Menu + "Buttons/Crimson", false, Priority + 41)]
        static void ButtonCrimson() => Create("Button_Crimson");

        [MenuItem(Menu + "Buttons/Olive", false, Priority + 42)]
        static void ButtonOlive() => Create("Button_Olive");

        [MenuItem(Menu + "Buttons/Parchment", false, Priority + 43)]
        static void ButtonParchment() => Create("Button_Parchment");

        [MenuItem(Menu + "Buttons/Leather", false, Priority + 44)]
        static void ButtonLeather() => Create("Button_Leather");

        [MenuItem(Menu + "Bars/Health", false, Priority + 60)]
        static void BarHealth() => Create("Bar_Health");

        [MenuItem(Menu + "Bars/Mana", false, Priority + 61)]
        static void BarMana() => Create("Bar_Mana");

        [MenuItem(Menu + "Bars/Stamina", false, Priority + 62)]
        static void BarStamina() => Create("Bar_Stamina");

        [MenuItem(Menu + "Bars/Experience", false, Priority + 63)]
        static void BarExperience() => Create("Bar_Experience");

        [MenuItem(Menu + "Slots/Emerald", false, Priority + 80)]
        static void SlotEmerald() => Create("Slot_Emerald");

        [MenuItem(Menu + "Slots/Obsidian", false, Priority + 81)]
        static void SlotObsidian() => Create("Slot_Obsidian");

        [MenuItem(Menu + "Slots/Crimson", false, Priority + 82)]
        static void SlotCrimson() => Create("Slot_Crimson");

        [MenuItem(Menu + "Slots/Bone", false, Priority + 83)]
        static void SlotBone() => Create("Slot_Bone");

        [MenuItem(Menu + "Widgets/Tooltip", false, Priority + 100)]
        static void Tooltip() => Create("Tooltip");

        [MenuItem(Menu + "Widgets/Toggle", false, Priority + 101)]
        static void Toggle() => Create("Toggle");

        [MenuItem(Menu + "Widgets/Slider", false, Priority + 102)]
        static void Slider() => Create("Slider");

        [MenuItem(Menu + "Widgets/Divider", false, Priority + 103)]
        static void Divider() => Create("Divider");

        [MenuItem("Tools/MyIndie.Games/Fantastic UI/Open Demo Scene", false, 20)]
        static void OpenDemo() => FantasticUIHub.OpenSceneAt(FantasticUIEditorUtility.DemoScene);

        [MenuItem("Tools/MyIndie.Games/Fantastic UI/Open Sprite Gallery", false, 21)]
        static void OpenGallery() => FantasticUIHub.OpenSceneAt(FantasticUIEditorUtility.GalleryScene);
    }
}
