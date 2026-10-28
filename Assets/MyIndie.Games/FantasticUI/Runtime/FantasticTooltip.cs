using TMPro;
using UnityEngine;

namespace MyIndieGames.FantasticUI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("MyIndie.Games/Fantastic UI/Fantastic Tooltip")]
    public class FantasticTooltip : MonoBehaviour
    {
        sealed class Registry
        {
            public FantasticTooltip Active;
        }

        static readonly Registry Current = new Registry();

        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text body;
        [SerializeField] Vector2 offset = new Vector2(22f, -22f);
        [SerializeField, Min(0f)] float fadeTime = 0.08f;
        [SerializeField, Min(0f)] float showDelay = 0.12f;

        CanvasGroup group;
        RectTransform rect;
        Canvas canvas;
        bool visible;
        float delay;
        Vector2? anchor;

        public static FantasticTooltip Active => Current.Active;

        public TMP_Text Title { get => title; set => title = value; }

        public TMP_Text Body { get => body; set => body = value; }

        public bool IsVisible => visible;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Current.Active = null;
        }

        void Awake()
        {
            group = GetComponent<CanvasGroup>();
            rect = (RectTransform)transform;
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        void OnEnable()
        {
            Current.Active = this;
        }

        void OnDisable()
        {
            if (Current.Active == this)
            {
                Current.Active = null;
            }
        }

        public static void Show(string heading, string text)
        {
            Show(heading, text, new Color(0.96f, 0.84f, 0.55f));
        }

        public static void Show(string heading, string text, Color headingColor)
        {
            var tooltip = Current.Active;
            if (tooltip == null)
            {
                return;
            }

            tooltip.anchor = null;
            tooltip.Display(heading, text, headingColor);
        }

        public static void ShowAt(string heading, string text, Color headingColor, Vector2 screenPosition)
        {
            var tooltip = Current.Active;
            if (tooltip == null)
            {
                return;
            }

            tooltip.anchor = screenPosition;
            tooltip.Display(heading, text, headingColor);
        }

        public static void Hide()
        {
            if (Current.Active != null)
            {
                Current.Active.visible = false;
                Current.Active.anchor = null;
            }
        }

        public void Display(string heading, string text, Color headingColor)
        {
            if (title != null)
            {
                title.text = heading;
                title.color = headingColor;
                title.gameObject.SetActive(!string.IsNullOrEmpty(heading));
            }

            if (body != null)
            {
                body.text = text;
                body.gameObject.SetActive(!string.IsNullOrEmpty(text));
            }

            if (!visible)
            {
                delay = showDelay;
            }

            visible = true;
            transform.SetAsLastSibling();
            Follow();
        }

        void LateUpdate()
        {
            if (visible)
            {
                if (delay > 0f)
                {
                    delay -= Time.unscaledDeltaTime;
                }

                Follow();
            }

            var target = visible && delay <= 0f ? 1f : 0f;
            group.alpha = fadeTime <= 0f ? target : Mathf.MoveTowards(group.alpha, target, Time.unscaledDeltaTime / fadeTime);
        }

        void Follow()
        {
            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>()?.rootCanvas;
                if (canvas == null)
                {
                    return;
                }
            }

            var canvasRect = (RectTransform)canvas.transform;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var pointer = anchor ?? FantasticPointer.Position;
            var right = pointer.x + offset.x + rect.rect.width * canvas.scaleFactor > canvas.pixelRect.width;
            var below = pointer.y + offset.y - rect.rect.height * canvas.scaleFactor >= 0f;
            rect.pivot = new Vector2(right ? 1f : 0f, below ? 1f : 0f);
            var shifted = pointer + new Vector2(right ? -offset.x : offset.x, below ? offset.y : -offset.y) * canvas.scaleFactor;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, shifted, camera, out var local))
            {
                rect.position = canvasRect.TransformPoint(local);
            }
        }
    }
}
