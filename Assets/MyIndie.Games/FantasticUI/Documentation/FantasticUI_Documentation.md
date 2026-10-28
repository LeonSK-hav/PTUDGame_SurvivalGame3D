# Fantastic UI Starter Pack 2.0

A free fantasy UI kit by **MyIndie.Games**: parchment, wood, leather and stone panels, buttons, labels, inventory slots, health and experience bars, more than 50 item icons and 3 silhouettes, with ready prefabs, a playable demo and small scripts that make them work.

Works with **uGUI + TextMeshPro** in Unity 2022.3 and Unity 6, with the Built-in Render Pipeline, URP and HDRP.

## Quick start

1. Import the package. The **Fantastic UI Hub** opens (Tools > MyIndie.Games > Fantastic UI).
2. If TextMeshPro asks for its **Essential Resources**, import them (or use the Hub > Setup button).
3. Open `Samples/Demo/FantasticUIDemo` and press **Play**.
4. Add pieces to your own scene from **GameObject > MyIndie.Games > Fantastic UI** or from the Hub > Create tab. A canvas and an event system are created for you when the scene has none.

## What is inside

| Folder | Content |
| --- | --- |
| `Sprites` | 126 sprites in Panels, Buttons, Labels, Slots, Bars, Widgets, Icons, Silhouettes, plus 6 cursors. Panels, buttons, labels and slots are 9-sliced. |
| `Sprites/FantasticUI.spriteatlas` | Packs every sprite into one atlas, so a full screen of UI draws in a few batches. |
| `Prefabs` | Windows (Parchment, Wood, Runestone, Emerald), Buttons, Bars (Health, Mana, Stamina, Experience), Slots, Tooltip, Toggle, Slider, Divider and a ready Canvas. |
| `Runtime` | The scripts below. |
| `Samples/Demo` | A full RPG screen: player frame, quick slots, inventory with drag and drop, character sheet with equipment and tabs, confirmation dialog, log. |
| `Samples/Gallery` | Browse every sprite by category, with a 9-slice preview. |

## Components

**Fantastic Bar** — health, mana, experience or any value bar. Set `Value` and `MaxValue` from code (`bar.Value -= 10`, `bar.SetValues(hp, maxHp)`), the fill moves smoothly and an optional *trail* image shows the lost amount for a moment. The label format accepts `{0}` value, `{1}` max and `{2}` percent. Works with Filled images or with sliced images (the fill is stretched by anchors).

**Fantastic Window** — open, close and toggle with a fade and scale animation (`Open()`, `Close()`, `Toggle()`, `IsOpen`). Clicking a window brings it to the front. Assign a close button and it is wired for you. Add **Fantastic Drag Handle** to a header to make it draggable inside the canvas.

**Fantastic Item** — a ScriptableObject (Create > MyIndie.Games > Fantastic UI > Item) with name, description, icon, tint, category, rarity, stack size, value and weight.

**Fantastic Slot** — shows an item and its count. Drag and drop between slots swaps or merges stacks. Restrict a slot to categories (for example a weapon slot) by turning off *Accept Any Category*. Right-click or double-click calls `OnUse`. Hovering shows the item tooltip.

**Fantastic Inventory** — groups the slots under it. `Add(item, count)` fills stacks then empty slots and returns what did not fit. Also `Remove`, `CountOf`, `TotalWeight`, `TotalValue`, `Clear` and an `OnChanged` event.

**Fantastic Tooltip** — one per canvas. Call `FantasticTooltip.Show(title, text)` and `Hide()` from anywhere, or add **Fantastic Tooltip Trigger** to any element. It follows the pointer and stays on screen. `ShowAt` pins it to a screen position (gamepad friendly).

**Fantastic Tab Group** — tab buttons with pages, selected and normal sprites.

**Fantastic Button Feedback** — hover and press scale, a glow, and optional hover and click sounds.

**Fantastic Cursor** — swaps the hardware cursor while the pointer is over an element, or on enable.

**Fantastic Event System** — put it on your EventSystem. It adds the Input System UI module when the Input System package is active and the Standalone module otherwise, so the samples work in any project.

**Fantastic Sprite Catalog** — lists every sprite by category and name (`catalog.Find("Heart_Red")`). The Hub > Sprites tab uses it: select a UI Image and click a sprite to apply it.

## Tips

- Icons whose name has no `_Color` suffix are white: tint them with the Image color.
- Use **Image Type: Sliced** for panels, buttons, labels and slots, and **Preserve Aspect** for icons and silhouettes.
- The canvas prefab uses a 1920 x 1080 reference resolution with a 0.5 width/height match.

## Support

Questions and ideas are welcome: **myindie.games**. If the pack helped you, a review on the Asset Store means a lot.

## Version history

- **2.0.0** (2026) — Remastered under MyIndie.Games. Every sprite separated, named and 9-sliced, new widgets, prefabs, scripts, demo, gallery, Hub and documentation.
- **1.0** (July 2018) — Original release.
