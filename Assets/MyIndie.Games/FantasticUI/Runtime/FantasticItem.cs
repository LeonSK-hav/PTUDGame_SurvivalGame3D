using UnityEngine;

namespace MyIndieGames.FantasticUI
{
    public enum FantasticItemCategory
    {
        Misc,
        Weapon,
        Tool,
        Consumable,
        Treasure,
        Quest,
        Material
    }

    public enum FantasticRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    [CreateAssetMenu(menuName = "MyIndie.Games/Fantastic UI/Item", fileName = "New Item", order = 0)]
    public class FantasticItem : ScriptableObject
    {
        [SerializeField] string displayName = "New Item";
        [SerializeField, TextArea(2, 5)] string description;
        [SerializeField] Sprite icon;
        [SerializeField] Color iconTint = Color.white;
        [SerializeField] FantasticItemCategory category;
        [SerializeField] FantasticRarity rarity;
        [SerializeField, Min(1)] int maxStack = 1;
        [SerializeField, Min(0)] int value;
        [SerializeField, Min(0f)] float weight;

        public string DisplayName { get => displayName; set => displayName = value; }

        public string Description { get => description; set => description = value; }

        public Sprite Icon { get => icon; set => icon = value; }

        public Color IconTint { get => iconTint; set => iconTint = value; }

        public FantasticItemCategory Category { get => category; set => category = value; }

        public FantasticRarity Rarity { get => rarity; set => rarity = value; }

        public int MaxStack { get => maxStack; set => maxStack = Mathf.Max(1, value); }

        public int Value { get => value; set => this.value = Mathf.Max(0, value); }

        public float Weight { get => weight; set => weight = Mathf.Max(0f, value); }

        public bool Stackable => maxStack > 1;

        public Color RarityColor => ColorOf(rarity);

        public static Color ColorOf(FantasticRarity rarity)
        {
            switch (rarity)
            {
                case FantasticRarity.Uncommon:
                    return new Color(0.45f, 0.86f, 0.36f);
                case FantasticRarity.Rare:
                    return new Color(0.36f, 0.62f, 1f);
                case FantasticRarity.Epic:
                    return new Color(0.78f, 0.45f, 1f);
                case FantasticRarity.Legendary:
                    return new Color(1f, 0.66f, 0.18f);
                default:
                    return new Color(0.93f, 0.89f, 0.8f);
            }
        }

        public string TooltipBody()
        {
            var text = "<color=#B9A27E><size=85%>" + rarity + " " + category + "</size></color>";
            if (!string.IsNullOrEmpty(description))
            {
                text += "\n" + description;
            }

            var details = string.Empty;
            if (value > 0)
            {
                details += value + " gold";
            }

            if (weight > 0f)
            {
                details += (details.Length > 0 ? "   " : string.Empty) + weight.ToString("0.#") + " kg";
            }

            if (details.Length > 0)
            {
                text += "\n<color=#DAA641><size=85%>" + details + "</size></color>";
            }

            return text;
        }
    }
}
