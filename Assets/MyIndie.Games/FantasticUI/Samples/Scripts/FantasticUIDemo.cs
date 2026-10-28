using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MyIndieGames.FantasticUI.Samples
{
    public class FantasticUIDemo : MonoBehaviour
    {
        [Serializable]
        public class ItemStack
        {
            public FantasticItem item;
            public int count = 1;
        }

        [SerializeField] FantasticBar health;
        [SerializeField] FantasticBar mana;
        [SerializeField] FantasticBar stamina;
        [SerializeField] FantasticBar experience;
        [SerializeField] TMP_Text levelLabel;
        [SerializeField] TMP_Text goldLabel;
        [SerializeField] TMP_Text weightLabel;
        [SerializeField] TMP_Text valueLabel;
        [SerializeField] TMP_Text logLabel;
        [SerializeField] TMP_Text attackLabel;
        [SerializeField] TMP_Text defenseLabel;
        [SerializeField] TMP_Text strengthLabel;
        [SerializeField] TMP_Text dexterityLabel;
        [SerializeField] TMP_Text intelligenceLabel;
        [SerializeField] TMP_Text pointsLabel;
        [SerializeField] FantasticInventory inventory;
        [SerializeField] List<FantasticSlot> equipment = new List<FantasticSlot>();
        [SerializeField] List<FantasticSlot> actionBar = new List<FantasticSlot>();
        [SerializeField] FantasticWindow inventoryWindow;
        [SerializeField] FantasticWindow characterWindow;
        [SerializeField] FantasticWindow confirmWindow;
        [SerializeField] List<ItemStack> startingItems = new List<ItemStack>();
        [SerializeField] List<ItemStack> actionItems = new List<ItemStack>();
        [SerializeField] List<ItemStack> startingEquipment = new List<ItemStack>();
        [SerializeField] FantasticItem healingItem;
        [SerializeField] FantasticItem foodItem;
        [SerializeField] FantasticItem manaItem;
        [SerializeField] float carryCapacity = 60f;

        readonly List<string> log = new List<string>();
        int level = 1;
        int gold = 120;
        int strength = 12;
        int dexterity = 10;
        int intelligence = 9;
        int points = 3;

        public int Level => level;

        public int Gold => gold;

        public FantasticInventory Inventory => inventory;

        public FantasticBar Health => health;

        public FantasticBar Mana => mana;

        public IReadOnlyList<FantasticSlot> Equipment => equipment;

        void Start()
        {
            FantasticEventSystem.Ensure();
            foreach (var slot in inventory.Slots)
            {
                slot.OnUse.AddListener(UseFromInventory);
            }

            foreach (var slot in equipment)
            {
                slot.OnUse.AddListener(Unequip);
                slot.OnChanged.AddListener(_ => RefreshStats());
            }

            foreach (var slot in actionBar)
            {
                slot.OnUse.AddListener(UseFromInventory);
            }

            inventory.OnChanged.AddListener(RefreshInventory);
            ResetDemo();
        }

        void Update()
        {
            var delta = Time.deltaTime;
            stamina.Add(6f * delta);
            mana.Add(1.5f * delta);
            if (DemoKeys.Pressed(KeyCode.I))
            {
                ToggleInventory();
            }

            if (DemoKeys.Pressed(KeyCode.C))
            {
                ToggleCharacter();
            }

            if (DemoKeys.Pressed(KeyCode.Escape))
            {
                CloseAll();
            }

            for (var i = 0; i < actionBar.Count && i < 6; i++)
            {
                if (DemoKeys.Pressed(KeyCode.Alpha1 + i))
                {
                    actionBar[i].Use();
                }
            }
        }

        public void ResetDemo()
        {
            level = 1;
            gold = 120;
            strength = 12;
            dexterity = 10;
            intelligence = 9;
            points = 3;
            inventory.Clear();
            foreach (var slot in equipment)
            {
                slot.Clear();
            }

            foreach (var slot in actionBar)
            {
                slot.Clear();
            }

            foreach (var stack in startingItems)
            {
                inventory.Add(stack.item, stack.count);
            }

            for (var i = 0; i < actionItems.Count && i < actionBar.Count; i++)
            {
                actionBar[i].Set(actionItems[i].item, actionItems[i].count);
            }

            for (var i = 0; i < startingEquipment.Count && i < equipment.Count; i++)
            {
                if (equipment[i].Accepts(startingEquipment[i].item))
                {
                    equipment[i].Set(startingEquipment[i].item, startingEquipment[i].count);
                }
            }

            experience.SetValues(35f, 100f, true);
            RefreshStats();
            health.SetValue(health.MaxValue * 0.72f, true);
            mana.SetValue(mana.MaxValue * 0.55f, true);
            stamina.SetValues(64f, 100f, true);
            log.Clear();
            Log("Welcome, traveller. Drag items, right-click to use them.");
            RefreshInventory();
        }

        public void TakeDamage()
        {
            var amount = UnityEngine.Random.Range(9, 19);
            health.Add(-amount);
            Log("<color=#E2574C>A goblin hits you for " + amount + ".</color>");
            if (health.Value <= 0f)
            {
                Log("You fall... and rise again at the shrine.");
                health.SetValue(health.MaxValue * 0.5f);
            }
        }

        public void Heal()
        {
            health.Add(20f);
            Log("<color=#8BDB5A>You bandage your wounds (+20).</color>");
        }

        public void CastSpell()
        {
            if (mana.Value < 15f)
            {
                Log("Not enough mana.");
                return;
            }

            mana.Add(-15f);
            stamina.Add(-10f);
            Log("<color=#6FA8FF>You cast Arcane Bolt.</color>");
            GainExperience(12f);
        }

        public void GainExperience()
        {
            GainExperience(25f);
        }

        public void GainExperience(float amount)
        {
            var total = experience.Value + amount;
            while (total >= experience.MaxValue)
            {
                total -= experience.MaxValue;
                level++;
                points += 3;
                experience.SetValues(0f, Mathf.Round(experience.MaxValue * 1.25f), true);
                Log("<color=#F2C14E>Level up! You reached level " + level + ".</color>");
            }

            experience.SetValue(total);
            RefreshStats();
        }

        public void ToggleInventory()
        {
            inventoryWindow.Toggle();
        }

        public void ToggleCharacter()
        {
            characterWindow.Toggle();
        }

        public void CloseAll()
        {
            inventoryWindow.Close();
            characterWindow.Close();
            confirmWindow.Close();
        }

        public void AskDropAll()
        {
            confirmWindow.Open();
        }

        public void ConfirmDropAll()
        {
            inventory.Clear();
            confirmWindow.Close();
            Log("You drop everything on the floor.");
        }

        public void CancelDropAll()
        {
            confirmWindow.Close();
        }

        public void Sort()
        {
            var stacks = new List<ItemStack>();
            foreach (var slot in inventory.Slots)
            {
                if (!slot.IsEmpty)
                {
                    stacks.Add(new ItemStack { item = slot.Item, count = slot.Count });
                }
            }

            stacks.Sort((a, b) =>
            {
                var byCategory = a.item.Category.CompareTo(b.item.Category);
                return byCategory != 0 ? byCategory : b.item.Rarity.CompareTo(a.item.Rarity);
            });
            inventory.Clear();
            foreach (var stack in stacks)
            {
                inventory.Add(stack.item, stack.count);
            }
        }

        public void AddStrength()
        {
            Spend(ref strength, "Strength");
        }

        public void AddDexterity()
        {
            Spend(ref dexterity, "Dexterity");
        }

        public void AddIntelligence()
        {
            Spend(ref intelligence, "Intelligence");
        }

        void Spend(ref int attribute, string label)
        {
            if (points <= 0)
            {
                Log("No attribute points left. Gain experience to level up.");
                return;
            }

            points--;
            attribute++;
            Log(label + " increased to " + attribute + ".");
            RefreshStats();
        }

        void UseFromInventory(FantasticSlot slot)
        {
            var item = slot.Item;
            if (item == null)
            {
                return;
            }

            if (item == healingItem)
            {
                health.Add(35f);
                slot.Remove();
                Log("<color=#8BDB5A>" + item.DisplayName + " restores 35 health.</color>");
                return;
            }

            if (item == foodItem)
            {
                health.Add(12f);
                stamina.Add(30f);
                slot.Remove();
                Log("You eat " + item.DisplayName + ".");
                return;
            }

            if (item == manaItem)
            {
                mana.Add(50f);
                slot.Remove();
                Log("<color=#6FA8FF>The runes glow. +50 mana.</color>");
                return;
            }

            foreach (var target in equipment)
            {
                if (target.IsEmpty && !target.AcceptAnyCategory && target.Accepts(item))
                {
                    FantasticSlot.Move(slot, target);
                    Log("Equipped " + item.DisplayName + ".");
                    return;
                }
            }

            Log(item.DisplayName + ": " + item.Description);
        }

        void Unequip(FantasticSlot slot)
        {
            if (slot.IsEmpty)
            {
                return;
            }

            var name = slot.Item.DisplayName;
            foreach (var target in inventory.Slots)
            {
                if (target.IsEmpty)
                {
                    FantasticSlot.Move(slot, target);
                    Log("Unequipped " + name + ".");
                    return;
                }
            }

            Log("Your bag is full.");
        }

        void RefreshStats()
        {
            var attack = strength / 2;
            var defense = dexterity / 2;
            foreach (var slot in equipment)
            {
                if (slot.IsEmpty)
                {
                    continue;
                }

                var bonus = (int)slot.Item.Rarity + 1;
                if (slot.Item.Category == FantasticItemCategory.Weapon)
                {
                    attack += bonus * 3;
                }
                else
                {
                    defense += bonus * 2;
                }
            }

            health.MaxValue = 80f + strength * 2f;
            mana.MaxValue = 30f + intelligence * 5f;
            Set(levelLabel, level.ToString());
            Set(attackLabel, attack.ToString());
            Set(defenseLabel, defense.ToString());
            Set(strengthLabel, strength.ToString());
            Set(dexterityLabel, dexterity.ToString());
            Set(intelligenceLabel, intelligence.ToString());
            Set(pointsLabel, points > 0 ? points + " points to spend" : "No points to spend");
        }

        void RefreshInventory()
        {
            var weight = inventory.TotalWeight;
            Set(weightLabel, weight.ToString("0.0") + " / " + carryCapacity.ToString("0") + " kg");
            if (weightLabel != null)
            {
                weightLabel.color = weight > carryCapacity ? new Color(0.9f, 0.35f, 0.3f) : new Color(0.33f, 0.22f, 0.12f);
            }

            Set(valueLabel, inventory.TotalValue + " gold worth");
            Set(goldLabel, gold.ToString());
        }

        void Log(string message)
        {
            log.Add(message);
            while (log.Count > 4)
            {
                log.RemoveAt(0);
            }

            Set(logLabel, string.Join("\n", log));
        }

        static void Set(TMP_Text label, string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }
    }
}
