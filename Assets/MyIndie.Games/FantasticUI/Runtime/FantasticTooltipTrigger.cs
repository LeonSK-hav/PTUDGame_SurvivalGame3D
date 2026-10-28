using UnityEngine;
using UnityEngine.EventSystems;

namespace MyIndieGames.FantasticUI
{
    [AddComponentMenu("MyIndie.Games/Fantastic UI/Fantastic Tooltip Trigger")]
    public class FantasticTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] string title;
        [SerializeField, TextArea(2, 6)] string body;
        [SerializeField] Color titleColor = new Color(0.96f, 0.84f, 0.55f);

        public string Title { get => title; set => title = value; }

        public string Body { get => body; set => body = value; }

        public Color TitleColor { get => titleColor; set => titleColor = value; }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!string.IsNullOrEmpty(title) || !string.IsNullOrEmpty(body))
            {
                FantasticTooltip.Show(title, body, titleColor);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            FantasticTooltip.Hide();
        }

        void OnDisable()
        {
            FantasticTooltip.Hide();
        }
    }
}
