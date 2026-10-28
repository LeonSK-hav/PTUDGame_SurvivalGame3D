using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MyIndieGames.FantasticUI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("MyIndie.Games/Fantastic UI/Fantastic Tab Group")]
    public class FantasticTabGroup : MonoBehaviour
    {
        [Serializable]
        public class Tab
        {
            public Button button;
            public GameObject page;
        }

        [Serializable]
        public class TabEvent : UnityEvent<int> { }

        [SerializeField] List<Tab> tabs = new List<Tab>();
        [SerializeField] Sprite selectedSprite;
        [SerializeField] Sprite normalSprite;
        [SerializeField] Color selectedTextColor = new Color(1f, 0.93f, 0.75f);
        [SerializeField] Color normalTextColor = new Color(0.72f, 0.62f, 0.48f);
        [SerializeField, Min(0)] int startIndex;
        [SerializeField] TabEvent onSelected = new TabEvent();

        int selected = -1;

        public List<Tab> Tabs => tabs;

        public int Selected => selected;

        public TabEvent OnSelected => onSelected;

        public Sprite SelectedSprite { get => selectedSprite; set => selectedSprite = value; }

        public Sprite NormalSprite { get => normalSprite; set => normalSprite = value; }

        void Awake()
        {
            for (var i = 0; i < tabs.Count; i++)
            {
                var index = i;
                if (tabs[i].button != null)
                {
                    tabs[i].button.onClick.AddListener(() => Select(index));
                }
            }
        }

        void Start()
        {
            if (selected < 0)
            {
                Select(Mathf.Clamp(startIndex, 0, Mathf.Max(0, tabs.Count - 1)));
            }
        }

        public void Select(int index)
        {
            if (tabs.Count == 0)
            {
                return;
            }

            index = Mathf.Clamp(index, 0, tabs.Count - 1);
            for (var i = 0; i < tabs.Count; i++)
            {
                var on = i == index;
                var tab = tabs[i];
                if (tab.page != null)
                {
                    tab.page.SetActive(on);
                }

                if (tab.button == null)
                {
                    continue;
                }

                if (tab.button.image != null)
                {
                    var sprite = on ? selectedSprite : normalSprite;
                    if (sprite != null)
                    {
                        tab.button.image.sprite = sprite;
                    }
                }

                var label = tab.button.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.color = on ? selectedTextColor : normalTextColor;
                }
            }

            var changed = selected != index;
            selected = index;
            if (changed)
            {
                onSelected.Invoke(index);
            }
        }
    }
}
