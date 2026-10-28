using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MyIndieGames.FantasticUI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("MyIndie.Games/Fantastic UI/Fantastic Slot")]
    public class FantasticSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [Serializable]
        public class SlotEvent : UnityEvent<FantasticSlot> { }

        sealed class DragState
        {
            public FantasticSlot Source;
            public RectTransform Ghost;
        }

        static readonly DragState Drag = new DragState();

        [SerializeField] Image icon;
        [SerializeField] Image placeholder;
        [SerializeField] Image highlight;
        [SerializeField] TMP_Text countLabel;
        [SerializeField] FantasticItem item;
        [SerializeField, Min(0)] int count = 1;
        [SerializeField] bool acceptAnyCategory = true;
        [SerializeField] List<FantasticItemCategory> acceptedCategories = new List<FantasticItemCategory>();
        [SerializeField] bool allowDrag = true;
        [SerializeField] bool showTooltip = true;
        [SerializeField] SlotEvent onChanged = new SlotEvent();
        [SerializeField] SlotEvent onUse = new SlotEvent();

        public FantasticItem Item => item;

        public int Count => item == null ? 0 : count;

        public bool IsEmpty => item == null;

        public bool AllowDrag { get => allowDrag; set => allowDrag = value; }

        public bool AcceptAnyCategory { get => acceptAnyCategory; set => acceptAnyCategory = value; }

        public List<FantasticItemCategory> AcceptedCategories => acceptedCategories;

        public Image Icon { get => icon; set { icon = value; Refresh(); } }

        public Image Placeholder { get => placeholder; set { placeholder = value; Refresh(); } }

        public Image Highlight { get => highlight; set { highlight = value; SetHighlight(false); } }

        public TMP_Text CountLabel { get => countLabel; set { countLabel = value; Refresh(); } }

        public SlotEvent OnChanged => onChanged;

        public SlotEvent OnUse => onUse;

        public static FantasticSlot Dragging => Drag.Source;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Drag.Source = null;
            Drag.Ghost = null;
        }

        void OnEnable()
        {
            Refresh();
            SetHighlight(false);
        }

        void OnValidate()
        {
            Refresh();
        }

        public bool Accepts(FantasticItem candidate)
        {
            return candidate == null || acceptAnyCategory || acceptedCategories.Contains(candidate.Category);
        }

        public void Set(FantasticItem newItem, int newCount = 1)
        {
            item = newItem;
            count = newItem == null ? 0 : Mathf.Clamp(newCount, 1, newItem.MaxStack);
            Refresh();
            onChanged.Invoke(this);
        }

        public void Clear()
        {
            Set(null, 0);
        }

        public int AddToStack(int amount)
        {
            if (item == null || amount <= 0)
            {
                return amount;
            }

            var room = item.MaxStack - count;
            var added = Mathf.Min(room, amount);
            if (added > 0)
            {
                count += added;
                Refresh();
                onChanged.Invoke(this);
            }

            return amount - added;
        }

        public void Remove(int amount = 1)
        {
            if (item == null)
            {
                return;
            }

            count -= Mathf.Max(0, amount);
            if (count <= 0)
            {
                Clear();
                return;
            }

            Refresh();
            onChanged.Invoke(this);
        }

        public void Use()
        {
            if (item != null)
            {
                onUse.Invoke(this);
            }
        }

        public void Refresh()
        {
            var hasItem = item != null;
            if (icon != null)
            {
                icon.sprite = hasItem ? item.Icon : null;
                icon.color = hasItem ? item.IconTint : Color.clear;
                icon.enabled = hasItem && item.Icon != null;
                icon.preserveAspect = true;
            }

            if (placeholder != null)
            {
                placeholder.enabled = !hasItem && placeholder.sprite != null;
            }

            if (countLabel != null)
            {
                countLabel.text = hasItem && count > 1 ? count.ToString() : string.Empty;
            }
        }

        public static bool Move(FantasticSlot from, FantasticSlot to)
        {
            if (from == null || to == null || from == to || from.item == null)
            {
                return false;
            }

            if (!to.Accepts(from.item))
            {
                return false;
            }

            if (to.item == from.item && to.item.Stackable)
            {
                var left = to.AddToStack(from.count);
                if (left == from.count)
                {
                    return false;
                }

                if (left <= 0)
                {
                    from.Clear();
                }
                else
                {
                    from.count = left;
                    from.Refresh();
                    from.onChanged.Invoke(from);
                }

                return true;
            }

            if (to.item != null && !from.Accepts(to.item))
            {
                return false;
            }

            var movingItem = from.item;
            var movingCount = from.count;
            from.Set(to.item, to.count);
            to.Set(movingItem, movingCount);
            return true;
        }

        void SetHighlight(bool on)
        {
            if (highlight != null)
            {
                highlight.enabled = on;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            var dragging = Drag.Source;
            SetHighlight(dragging == null || (dragging != this && Accepts(dragging.item)));
            if (showTooltip && item != null && dragging == null)
            {
                FantasticTooltip.Show(item.DisplayName, item.TooltipBody(), item.RarityColor);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHighlight(false);
            if (showTooltip)
            {
                FantasticTooltip.Hide();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right || eventData.clickCount >= 2)
            {
                Use();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!allowDrag || item == null || eventData.button != PointerEventData.InputButton.Left)
            {
                eventData.pointerDrag = null;
                return;
            }

            FantasticTooltip.Hide();
            Drag.Source = this;
            Drag.Ghost = CreateGhost();
            if (icon != null)
            {
                icon.color = item.IconTint * new Color(1f, 1f, 1f, 0.35f);
            }

            MoveGhost(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Drag.Source == this)
            {
                MoveGhost(eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (Drag.Ghost != null)
            {
                Destroy(Drag.Ghost.gameObject);
            }

            Drag.Ghost = null;
            Drag.Source = null;
            Refresh();
        }

        public void OnDrop(PointerEventData eventData)
        {
            var source = Drag.Source;
            SetHighlight(false);
            if (source != null && source != this)
            {
                Move(source, this);
            }
        }

        RectTransform CreateGhost()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null || icon == null)
            {
                return null;
            }

            var root = canvas.rootCanvas.transform;
            var ghost = new GameObject("Drag Ghost", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            var rect = (RectTransform)ghost.transform;
            rect.SetParent(root, false);
            rect.SetAsLastSibling();
            rect.sizeDelta = icon.rectTransform.rect.size * 1.1f;
            var image = ghost.GetComponent<Image>();
            image.sprite = item.Icon;
            image.color = item.IconTint;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var group = ghost.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.alpha = 0.9f;
            return rect;
        }

        static void MoveGhost(PointerEventData eventData)
        {
            var ghost = Drag.Ghost;
            if (ghost == null)
            {
                return;
            }

            var parent = (RectTransform)ghost.parent;
            var canvas = parent.GetComponent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, eventData.position, camera, out var world))
            {
                ghost.position = world;
            }
        }
    }
}
