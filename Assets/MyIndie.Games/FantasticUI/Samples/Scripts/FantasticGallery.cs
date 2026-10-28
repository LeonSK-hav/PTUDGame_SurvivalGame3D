using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyIndieGames.FantasticUI.Samples
{
    public class FantasticGallery : MonoBehaviour
    {
        [SerializeField] FantasticSpriteCatalog catalog;
        [SerializeField] RectTransform tabContainer;
        [SerializeField] Button tabTemplate;
        [SerializeField] RectTransform grid;
        [SerializeField] Button cellTemplate;
        [SerializeField] Image preview;
        [SerializeField] Image slicedPreview;
        [SerializeField] TMP_Text previewName;
        [SerializeField] TMP_Text previewInfo;
        [SerializeField] TMP_Text countLabel;
        [SerializeField] Sprite tabNormal;
        [SerializeField] Sprite tabSelected;
        [SerializeField] List<string> hiddenCategories = new List<string> { "Demo" };

        readonly List<Button> tabs = new List<Button>();
        readonly List<GameObject> cells = new List<GameObject>();
        List<string> categories = new List<string>();

        public int CellCount => cells.Count;

        public string CurrentCategory { get; private set; }

        void Start()
        {
            FantasticEventSystem.Ensure();
            tabTemplate.gameObject.SetActive(false);
            cellTemplate.gameObject.SetActive(false);
            categories = catalog.Categories();
            categories.RemoveAll(hiddenCategories.Contains);
            foreach (var category in categories)
            {
                var tab = Instantiate(tabTemplate, tabContainer);
                tab.gameObject.SetActive(true);
                tab.name = "Tab " + category;
                tab.GetComponentInChildren<TMP_Text>().text = category;
                var captured = category;
                tab.onClick.AddListener(() => Show(captured));
                tabs.Add(tab);
            }

            if (categories.Count > 0)
            {
                Show(categories[0]);
            }
        }

        public void Show(string category)
        {
            CurrentCategory = category;
            foreach (var cell in cells)
            {
                Destroy(cell);
            }

            cells.Clear();
            for (var i = 0; i < tabs.Count; i++)
            {
                var on = categories[i] == category;
                tabs[i].image.sprite = on ? tabSelected : tabNormal;
                tabs[i].GetComponentInChildren<TMP_Text>().color = on ? new Color(1f, 0.92f, 0.72f) : new Color(0.78f, 0.68f, 0.52f);
            }

            FantasticSpriteCatalog.Entry first = null;
            foreach (var entry in catalog.InCategory(category))
            {
                var cell = Instantiate(cellTemplate, grid);
                cell.gameObject.SetActive(true);
                cell.name = entry.name;
                var icon = cell.transform.Find("Icon").GetComponent<Image>();
                icon.sprite = entry.sprite;
                icon.preserveAspect = true;
                cell.GetComponentInChildren<TMP_Text>().text = entry.name.Replace('_', ' ');
                var captured = entry;
                cell.onClick.AddListener(() => Select(captured));
                cells.Add(cell.gameObject);
                first ??= entry;
            }

            if (countLabel != null)
            {
                countLabel.text = cells.Count + " sprites";
            }

            if (first != null)
            {
                Select(first);
            }
        }

        public void Select(FantasticSpriteCatalog.Entry entry)
        {
            var sprite = entry.sprite;
            preview.sprite = sprite;
            preview.preserveAspect = true;
            var sliced = sprite != null && sprite.border.sqrMagnitude > 0f;
            slicedPreview.gameObject.SetActive(sliced);
            if (sliced)
            {
                slicedPreview.sprite = sprite;
                slicedPreview.type = Image.Type.Sliced;
            }

            previewName.text = entry.name.Replace('_', ' ');
            if (sprite == null)
            {
                previewInfo.text = string.Empty;
                return;
            }

            var size = sprite.rect.size;
            var info = entry.category + "  •  " + (int)size.x + " x " + (int)size.y + " px";
            info += sliced ? "\n9-slice ready: stretch it to any size" : "\nUse Preserve Aspect";
            previewInfo.text = info;
        }
    }
}
