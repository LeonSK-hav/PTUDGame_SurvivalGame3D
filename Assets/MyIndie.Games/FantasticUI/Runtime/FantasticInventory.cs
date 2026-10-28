using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MyIndieGames.FantasticUI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("MyIndie.Games/Fantastic UI/Fantastic Inventory")]
    public class FantasticInventory : MonoBehaviour
    {
        [SerializeField] List<FantasticSlot> slots = new List<FantasticSlot>();
        [SerializeField] UnityEvent onChanged = new UnityEvent();

        public IReadOnlyList<FantasticSlot> Slots
        {
            get
            {
                Collect();
                return slots;
            }
        }

        public UnityEvent OnChanged => onChanged;

        public float TotalWeight
        {
            get
            {
                var total = 0f;
                foreach (var slot in Slots)
                {
                    if (slot != null && slot.Item != null)
                    {
                        total += slot.Item.Weight * slot.Count;
                    }
                }

                return total;
            }
        }

        public int TotalValue
        {
            get
            {
                var total = 0;
                foreach (var slot in Slots)
                {
                    if (slot != null && slot.Item != null)
                    {
                        total += slot.Item.Value * slot.Count;
                    }
                }

                return total;
            }
        }

        void Awake()
        {
            Collect();
        }

        void OnEnable()
        {
            foreach (var slot in Slots)
            {
                if (slot != null)
                {
                    slot.OnChanged.AddListener(Changed);
                }
            }
        }

        void OnDisable()
        {
            foreach (var slot in slots)
            {
                if (slot != null)
                {
                    slot.OnChanged.RemoveListener(Changed);
                }
            }
        }

        void Collect()
        {
            slots.RemoveAll(slot => slot == null);
            if (slots.Count == 0)
            {
                GetComponentsInChildren(true, slots);
            }
        }

        void Changed(FantasticSlot slot)
        {
            onChanged.Invoke();
        }

        public int Add(FantasticItem item, int amount = 1)
        {
            if (item == null || amount <= 0)
            {
                return amount;
            }

            foreach (var slot in Slots)
            {
                if (amount <= 0)
                {
                    break;
                }

                if (slot.Item == item && item.Stackable)
                {
                    amount = slot.AddToStack(amount);
                }
            }

            foreach (var slot in Slots)
            {
                if (amount <= 0)
                {
                    break;
                }

                if (slot.IsEmpty && slot.Accepts(item))
                {
                    var placed = Mathf.Min(amount, item.MaxStack);
                    slot.Set(item, placed);
                    amount -= placed;
                }
            }

            return amount;
        }

        public int CountOf(FantasticItem item)
        {
            var total = 0;
            foreach (var slot in Slots)
            {
                if (slot.Item == item)
                {
                    total += slot.Count;
                }
            }

            return total;
        }

        public bool Remove(FantasticItem item, int amount = 1)
        {
            if (CountOf(item) < amount)
            {
                return false;
            }

            for (var i = Slots.Count - 1; i >= 0 && amount > 0; i--)
            {
                var slot = slots[i];
                if (slot.Item != item)
                {
                    continue;
                }

                var taken = Mathf.Min(amount, slot.Count);
                slot.Remove(taken);
                amount -= taken;
            }

            return true;
        }

        public void Clear()
        {
            foreach (var slot in Slots)
            {
                slot.Clear();
            }
        }
    }
}
